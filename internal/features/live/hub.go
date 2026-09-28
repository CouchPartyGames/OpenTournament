// Package live is the live-update slice: one WebSocket per client, on which
// it subscribes to any number of Tournaments. Events come from the outbox
// (ADR-0003): each replica LISTENs for NOTIFYs carrying a Tournament ID and
// sequence number, reads the new rows, and forwards them to its own sockets.
// There is no replay: clients refetch over REST on (re)connect or on a gap.
package live

import (
	"context"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"sync"
	"time"

	"github.com/coder/websocket"
	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

// Hub forwards outbox events to subscribed sockets.
type Hub struct {
	pool     *pgxpool.Pool
	q        *db.Queries
	verifier auth.Verifier

	mu     sync.Mutex
	topics map[ids.TournamentID]*topic
	// deliverMu serializes reading the outbox, so events go out in order.
	deliverMu sync.Mutex

	// Poll is how often subscribed Tournaments are checked even without a
	// NOTIFY, covering notifications lost while reconnecting.
	Poll time.Duration
}

type topic struct {
	lastSeq int64
	clients map[*client]bool
}

// NewHub returns a Hub.
func NewHub(pool *pgxpool.Pool, verifier auth.Verifier) *Hub {
	return &Hub{pool: pool, q: db.New(pool), verifier: verifier, topics: map[ids.TournamentID]*topic{}, Poll: 2 * time.Second}
}

// Run listens for outbox notifications until ctx ends, reconnecting on failure.
func (h *Hub) Run(ctx context.Context) {
	go func() {
		t := time.NewTicker(h.Poll)
		defer t.Stop()
		for {
			select {
			case <-ctx.Done():
				return
			case <-t.C:
				h.deliverAll(ctx)
			}
		}
	}()
	for ctx.Err() == nil {
		if err := h.listen(ctx); err != nil && ctx.Err() == nil {
			slog.WarnContext(ctx, "live updates: listen failed; reconnecting", "error", err)
			select {
			case <-ctx.Done():
			case <-time.After(time.Second):
			}
		}
	}
}

func (h *Hub) listen(ctx context.Context) error {
	pc, err := h.pool.Acquire(ctx)
	if err != nil {
		return err
	}
	conn := pc.Hijack()
	defer conn.Close(context.WithoutCancel(ctx))
	if _, err := conn.Exec(ctx, "LISTEN "+pgx.Identifier{events.Channel}.Sanitize()); err != nil {
		return err
	}
	// Catch up on anything missed before LISTEN took effect.
	h.deliverAll(ctx)
	for {
		n, err := conn.WaitForNotification(ctx)
		if err != nil {
			return err
		}
		tid, _, err := events.ParsePayload(n.Payload)
		if err != nil {
			continue
		}
		h.deliver(ctx, tid)
	}
}

func (h *Hub) deliverAll(ctx context.Context) {
	h.mu.Lock()
	tids := make([]ids.TournamentID, 0, len(h.topics))
	for tid := range h.topics {
		tids = append(tids, tid)
	}
	h.mu.Unlock()
	for _, tid := range tids {
		h.deliver(ctx, tid)
	}
}

// deliver forwards a Tournament's events that its sockets haven't seen.
func (h *Hub) deliver(ctx context.Context, tid ids.TournamentID) {
	h.deliverMu.Lock()
	defer h.deliverMu.Unlock()
	for {
		h.mu.Lock()
		t, ok := h.topics[tid]
		var after int64
		if ok {
			after = t.lastSeq
		}
		h.mu.Unlock()
		if !ok {
			return
		}
		evs, err := h.q.ListEventsAfter(ctx, db.ListEventsAfterParams{TournamentID: tid, Seq: after, Limit: 500})
		if err != nil {
			if ctx.Err() == nil {
				slog.WarnContext(ctx, "live updates: read events", "tournament", tid, "error", err)
			}
			return
		}
		if len(evs) == 0 {
			return
		}
		h.mu.Lock()
		t, ok = h.topics[tid]
		if ok {
			for _, e := range evs {
				for c := range t.clients {
					c.sendEvent(e)
				}
				t.lastSeq = e.Seq
			}
		}
		h.mu.Unlock()
	}
}

func (h *Hub) subscribe(ctx context.Context, c *client, tid ids.TournamentID) (int64, error) {
	h.mu.Lock()
	t, ok := h.topics[tid]
	h.mu.Unlock()
	if !ok {
		seq, err := h.q.LastEventSeq(ctx, tid)
		if err != nil {
			return 0, err
		}
		h.mu.Lock()
		if t, ok = h.topics[tid]; !ok {
			t = &topic{lastSeq: seq, clients: map[*client]bool{}}
			h.topics[tid] = t
		}
		h.mu.Unlock()
	}
	h.mu.Lock()
	defer h.mu.Unlock()
	t.clients[c] = true
	return t.lastSeq, nil
}

func (h *Hub) unsubscribe(c *client, tid ids.TournamentID) {
	h.mu.Lock()
	defer h.mu.Unlock()
	if t, ok := h.topics[tid]; ok {
		delete(t.clients, c)
		if len(t.clients) == 0 {
			delete(h.topics, tid)
		}
	}
}

// ClientMessage is what a client sends.
type ClientMessage struct {
	Type         string `json:"type"` // authenticate, subscribe, unsubscribe
	Token        string `json:"token,omitempty"`
	TournamentID string `json:"tournamentId,omitempty"`
}

// ServerMessage is what the server sends.
type ServerMessage struct {
	Type         string            `json:"type"` // authenticated, subscribed, unsubscribed, event, error
	TournamentID *ids.TournamentID `json:"tournamentId,omitempty"`
	// Seq is the event's sequence number, or on subscribed the last one
	// already sent: fetch state over REST, then apply newer events.
	Seq   int64           `json:"seq,omitempty"`
	Event string          `json:"event,omitempty"`
	Data  json.RawMessage `json:"data,omitempty"`
	Code  string          `json:"code,omitempty"`
	Error string          `json:"error,omitempty"`
}

type client struct {
	send      chan []byte
	principal *auth.Principal
	close     func()
}

// sendEvent queues an event, with its private part only for its recipients.
// A client too slow to keep up is disconnected; it will refetch.
func (c *client) sendEvent(e db.Event) {
	data := e.Data
	if e.Private != nil && c.principal != nil && c.allowed(e.Recipients) {
		data = merge(e.Data, e.Private)
	}
	tid := e.TournamentID
	c.enqueue(ServerMessage{Type: "event", TournamentID: &tid, Seq: e.Seq, Event: e.Type, Data: data})
}

func (c *client) allowed(recipients []string) bool {
	for _, k := range c.principal.Keys() {
		for _, r := range recipients {
			if k == r {
				return true
			}
		}
	}
	return false
}

func (c *client) enqueue(m ServerMessage) {
	b, _ := json.Marshal(m) // can't fail: m holds only JSON-safe values
	select {
	case c.send <- b:
	default:
		c.close()
	}
}

func merge(public, private json.RawMessage) json.RawMessage {
	var a, b map[string]any
	if json.Unmarshal(public, &a) != nil || json.Unmarshal(private, &b) != nil {
		return public
	}
	for k, v := range b {
		a[k] = v
	}
	out, _ := json.Marshal(a) // can't fail: a was just unmarshalled from JSON
	return out
}

// ServeHTTP upgrades to a WebSocket and serves one client.
func (h *Hub) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	conn, err := websocket.Accept(w, r, &websocket.AcceptOptions{InsecureSkipVerify: true})
	if err != nil {
		return
	}
	ctx, cancel := context.WithCancel(r.Context())
	defer cancel()
	c := &client{send: make(chan []byte, 256), close: cancel}
	subs := map[ids.TournamentID]bool{}
	defer func() {
		for tid := range subs {
			h.unsubscribe(c, tid)
		}
		conn.CloseNow()
	}()

	go func() {
		for {
			select {
			case <-ctx.Done():
				return
			case b := <-c.send:
				wctx, wcancel := context.WithTimeout(ctx, 10*time.Second)
				err := conn.Write(wctx, websocket.MessageText, b)
				wcancel()
				if err != nil {
					cancel()
					return
				}
			}
		}
	}()

	first := true
	for {
		_, b, err := conn.Read(ctx)
		if err != nil {
			return
		}
		var m ClientMessage
		if err := json.Unmarshal(b, &m); err != nil {
			c.enqueue(ServerMessage{Type: "error", Code: "bad-message", Error: "messages must be JSON"})
			continue
		}
		switch m.Type {
		case "authenticate":
			if !first {
				c.enqueue(ServerMessage{Type: "error", Code: "authenticate-first", Error: "authenticate must be the first message"})
				continue
			}
			p, err := h.verifier.Verify(ctx, m.Token)
			if err != nil {
				c.enqueue(ServerMessage{Type: "error", Code: "unauthenticated", Error: "the token is invalid or expired"})
				conn.Close(websocket.StatusPolicyViolation, "unauthenticated")
				return
			}
			h.mu.Lock()
			c.principal = &p
			h.mu.Unlock()
			c.enqueue(ServerMessage{Type: "authenticated"})
		case "subscribe", "unsubscribe":
			tid, err := ids.Parse[ids.TournamentID](m.TournamentID)
			if err != nil {
				c.enqueue(ServerMessage{Type: "error", Code: "bad-message", Error: "tournamentId must be a UUID"})
				continue
			}
			if m.Type == "unsubscribe" {
				h.unsubscribe(c, tid)
				delete(subs, tid)
				c.enqueue(ServerMessage{Type: "unsubscribed", TournamentID: &tid})
				continue
			}
			seq, err := h.subscribe(ctx, c, tid)
			if errors.Is(err, pgx.ErrNoRows) {
				c.enqueue(ServerMessage{Type: "error", TournamentID: &tid, Code: "tournament-not-found", Error: "no such tournament"})
				continue
			} else if err != nil {
				c.enqueue(ServerMessage{Type: "error", TournamentID: &tid, Code: "internal-error", Error: "subscribe failed"})
				continue
			}
			subs[tid] = true
			c.enqueue(ServerMessage{Type: "subscribed", TournamentID: &tid, Seq: seq})
		default:
			c.enqueue(ServerMessage{Type: "error", Code: "bad-message", Error: "unknown message type " + m.Type})
		}
		first = false
	}
}
