package app_test

import (
	"context"
	"encoding/json"
	"strings"
	"testing"
	"time"

	"net/http"

	"github.com/coder/websocket"
	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

type liveMessage struct {
	Type         string          `json:"type"`
	TournamentID string          `json:"tournamentId"`
	Seq          int64           `json:"seq"`
	Event        string          `json:"event"`
	Data         json.RawMessage `json:"data"`
	Code         string          `json:"code"`
}

type socket struct {
	t    *testing.T
	conn *websocket.Conn
}

func connect(t *testing.T, h *apptest.Harness) *socket {
	t.Helper()
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	conn, _, err := websocket.Dial(ctx, "ws"+strings.TrimPrefix(h.URL, "http")+"/api/v1/live", nil)
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { conn.CloseNow() })
	return &socket{t: t, conn: conn}
}

func (s *socket) send(m map[string]any) {
	s.t.Helper()
	b, _ := json.Marshal(m)
	if err := s.conn.Write(context.Background(), websocket.MessageText, b); err != nil {
		s.t.Fatal(err)
	}
}

// next reads messages until one matches, failing after a timeout.
func (s *socket) next(match func(liveMessage) bool) liveMessage {
	s.t.Helper()
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	for {
		_, b, err := s.conn.Read(ctx)
		if err != nil {
			s.t.Fatalf("no matching message: %v", err)
		}
		var m liveMessage
		json.Unmarshal(b, &m)
		if match(m) {
			return m
		}
	}
}

func isEvent(name string) func(liveMessage) bool {
	return func(m liveMessage) bool { return m.Type == "event" && m.Event == name }
}

func (s *socket) subscribe(tournament string) liveMessage {
	s.t.Helper()
	s.send(map[string]any{"type": "subscribe", "tournamentId": tournament})
	return s.next(func(m liveMessage) bool { return m.Type == "subscribed" || m.Type == "error" })
}

func TestSpectatorFollowsRegistrationsLiveWithSequenceNumbers(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	tt.openRegistration()
	ws := connect(t, h)
	sub := ws.subscribe(tt.ID)

	tt.mustRegister("anna")
	first := ws.next(isEvent("registrations.changed"))
	tt.mustRegister("bert")
	second := ws.next(isEvent("registrations.changed"))

	var counts struct{ Registered int }
	json.Unmarshal(second.Data, &counts)
	if counts.Registered != 2 {
		t.Fatalf("data = %s, want 2 registered", second.Data)
	}
	if first.Seq != sub.Seq+1 || second.Seq != first.Seq+1 {
		t.Fatalf("seqs subscribed=%d first=%d second=%d, want consecutive", sub.Seq, first.Seq, second.Seq)
	}
}

func TestOnlyTheMatchesParticipantsLearnTheGameServerAddressLive(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	tt.openRegistration()
	ps := tt.mustRegister("anna", "bert")
	anna := connect(t, h)
	anna.send(map[string]any{"type": "authenticate", "token": tt.Tokens[ps[0]]})
	anna.next(func(m liveMessage) bool { return m.Type == "authenticated" })
	anna.subscribe(tt.ID)
	spectator := connect(t, h)
	spectator.subscribe(tt.ID)

	tt.start()

	allocated := func(m liveMessage) bool {
		var d struct{ ServerAllocated bool }
		json.Unmarshal(m.Data, &d)
		return isEvent("match.changed")(m) && d.ServerAllocated
	}
	private := anna.next(allocated)
	public := spectator.next(allocated)
	var pd, sd struct {
		ServerAddress string `json:"serverAddress"`
		ServerPort    int    `json:"serverPort"`
	}
	json.Unmarshal(private.Data, &pd)
	json.Unmarshal(public.Data, &sd)
	if pd.ServerAddress == "" || pd.ServerPort == 0 {
		t.Fatalf("anna got %s, want the server address", private.Data)
	}
	if sd.ServerAddress != "" {
		t.Fatalf("spectator got %s, want no address", public.Data)
	}
	if private.Seq != public.Seq {
		t.Fatalf("same event has seq %d and %d", private.Seq, public.Seq)
	}
}

func TestAuthenticationMustBeTheFirstMessage(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	ws := connect(t, h)
	ws.subscribe(tt.ID)

	ws.send(map[string]any{"type": "authenticate", "token": h.User("anna")})

	if m := ws.next(func(m liveMessage) bool { return m.Type == "error" }); m.Code != "authenticate-first" {
		t.Fatalf("got %+v", m)
	}
}

func TestSubscribingToAnUnknownTournamentFails(t *testing.T) {
	h := apptest.MustStart(t)
	ws := connect(t, h)

	if m := ws.subscribe("01a0e85d-6bc9-748c-a3e1-a1ab27040c5e"); m.Code != "tournament-not-found" {
		t.Fatalf("got %+v", m)
	}
}

func TestFreeForAllStandingsGoLiveAfterEveryBout(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 3}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	ps := tt.mustRegister("anna", "bert")
	tt.start()
	ws := connect(t, h)
	ws.subscribe(tt.ID)
	m := tt.matches("allocating")[0]

	tt.reportBout(tt.server(m.ID), 1, map[string]any{"placements": []map[string]any{
		{"participantId": ps[0], "placement": 1, "points": 7},
		{"participantId": ps[1], "placement": 2, "points": 2},
	}}).Expect(http.StatusNoContent)

	ev := ws.next(isEvent("standings.changed"))
	var d struct {
		Standings []standingView `json:"standings"`
	}
	json.Unmarshal(ev.Data, &d)
	if len(d.Standings) != 2 || d.Standings[0].Points != 7 {
		t.Fatalf("standings = %s, want anna on 7 points", ev.Data)
	}
}

func TestAbortIsAnnouncedLive(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]
	ws := connect(t, tt.Harness)
	ws.subscribe(tt.ID)

	tt.FakeServers.MakeUnhealthy(serverName(tt, final.ID))
	tt.Settle()

	ev := ws.next(func(m liveMessage) bool {
		var d struct{ Aborted bool }
		json.Unmarshal(m.Data, &d)
		return isEvent("match.changed")(m) && d.Aborted
	})
	var d struct{ Aborts int }
	json.Unmarshal(ev.Data, &d)
	if d.Aborts != 1 {
		t.Fatalf("event = %s, want aborts 1", ev.Data)
	}
}

func TestTournamentStatusChangesAreAnnouncedLive(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	ws := connect(t, h)
	ws.subscribe(tt.ID)
	status := func(ev liveMessage) string {
		t.Helper()
		var d struct{ Status string }
		if err := json.Unmarshal(ev.Data, &d); err != nil {
			t.Fatalf("%s data %s: %v", ev.Event, ev.Data, err)
		}
		return d.Status
	}

	tt.openRegistration()
	if got := status(ws.next(isEvent("tournament.status-changed"))); got != "registration-open" {
		t.Errorf("status-changed to %q, want registration-open", got)
	}
	tt.mustRegister("anna", "bert")
	tt.start()
	if got := status(ws.next(isEvent("tournament.status-changed"))); got != "running" {
		t.Errorf("status-changed to %q, want running", got)
	}
	tt.playAll(tt.byName)
	if got := status(ws.next(isEvent("tournament.completed"))); got != "completed" {
		t.Errorf("completed with status %q, want completed", got)
	}

	var placements struct{ Status string }
	h.Do(http.MethodGet, tt.path("placements"), "", nil).Expect(http.StatusOK).Decode(&placements)
	if placements.Status != "completed" {
		t.Errorf("Final Placements status = %q, want completed", placements.Status)
	}
}

func TestParticipantDeparturesAreAnnouncedLive(t *testing.T) {
	tt := mustRun(t, 4)
	ws := connect(t, tt.Harness)
	ws.subscribe(tt.ID)
	ps := tt.matches("allocating")[0].Participants
	change := func() (participant, status string) {
		t.Helper()
		ev := ws.next(isEvent("participant.changed"))
		var d struct{ ParticipantID, Status string }
		if err := json.Unmarshal(ev.Data, &d); err != nil {
			t.Fatalf("%s data %s: %v", ev.Event, ev.Data, err)
		}
		return d.ParticipantID, d.Status
	}

	tt.withdraw(ps[0]).Expect(http.StatusNoContent)
	if p, s := change(); p != ps[0] || s != "withdrawn" {
		t.Errorf("participant.changed %s to %q, want %s withdrawn", p, s, ps[0])
	}
	tt.Do(http.MethodPost, tt.path("participants", ps[1], "disqualify"), tt.Organizer, nil).Expect(http.StatusNoContent)
	if p, s := change(); p != ps[1] || s != "disqualified" {
		t.Errorf("participant.changed %s to %q, want %s disqualified", p, s, ps[1])
	}
}
