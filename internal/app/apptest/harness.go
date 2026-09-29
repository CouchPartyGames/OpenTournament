// Package apptest hosts the real app in-process against a real PostgreSQL,
// with controlled test doubles: a fake clock, a fake Game Server port, a
// seeded random source, a test JWT issuer in place of Keycloak, and
// client-go's fake dynamic client in place of the Kubernetes API.
package apptest

import (
	"bytes"
	"context"
	"encoding/json"
	"io"
	"log/slog"
	"math/rand/v2"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app"
	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/auth/authtest"
	"github.com/couchpartygames/opentournament/internal/clock/clocktest"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/gameserver/gameservertest"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/jackc/pgx/v5/pgxpool"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime"
	"k8s.io/apimachinery/pkg/runtime/schema"
	dynamicfake "k8s.io/client-go/dynamic/fake"
	"k8s.io/client-go/tools/cache"
)

// Epoch is where the fake clock starts.
var Epoch = time.Date(2030, 1, 1, 12, 0, 0, 0, time.UTC)

// Catalog is the Game catalog tests run with.
var Catalog = games.Config{
	Games: []games.Game{
		{
			ID: "arena", Name: "Arena", IdentityKinds: []string{"keycloak", "steam"}, MaximumMatchSize: 2,
			Fleet: games.Fleet{Name: "arena", Namespace: "games"}, TrustedClients: []string{"arena-backend"},
		},
		{
			ID: "royale", Name: "Royale", IdentityKinds: []string{"keycloak"}, MaximumMatchSize: 8,
			Fleet: games.Fleet{Name: "royale", Namespace: "games"}, TrustedClients: []string{"royale-backend"},
		},
	},
	IdentityClaims: map[string]string{"steam": "steam_id"},
}

// Harness is a running app.
type Harness struct {
	t           testing.TB
	URL         string
	App         *app.App
	FakeClock   *clocktest.Fake
	FakeServers *gameservertest.Fake
	// FakeKubernetes holds the Tournament Manifests and Recurring
	// Tournaments. The app reads them only in the namespaces WatchManifests
	// names.
	FakeKubernetes *dynamicfake.FakeDynamicClient
	Issuer         *authtest.Issuer
	Pool           *pgxpool.Pool
}

// Option configures the hosted app.
type Option func(*app.Config)

// WatchManifests makes the app declare the Tournament Manifests and
// Recurring Tournaments in these namespaces. Without it, no namespace is watched.
func WatchManifests(namespaces ...string) Option {
	return func(c *app.Config) { c.ManifestNamespaces = namespaces }
}

// MustStart hosts the app for one test. Background workers other than live
// updates don't run: tests drive time and reconciliation with Advance and
// Settle, so every scenario is deterministic.
func MustStart(t testing.TB, opts ...Option) *Harness {
	t.Helper()
	ctx, cancel := context.WithCancel(context.Background())
	pool, err := pgxpool.New(ctx, newDatabase(t))
	if err != nil {
		t.Fatal(err)
	}
	issuer := authtest.Start()
	catalog, err := games.New(Catalog)
	if err != nil {
		t.Fatal(err)
	}
	verifier, err := auth.NewOIDC(ctx, issuer.URL, catalog.IdentityClaims)
	if err != nil {
		t.Fatal(err)
	}
	h := &Harness{
		t: t, FakeClock: clocktest.NewFake(Epoch), FakeServers: gameservertest.NewFake(), Issuer: issuer, Pool: pool,
		FakeKubernetes: dynamicfake.NewSimpleDynamicClientWithCustomListKinds(runtime.NewScheme(),
			map[schema.GroupVersionResource]string{
				manifests.Resource:          "TournamentList",
				manifests.RecurringResource: "RecurringTournamentList",
			}),
	}
	cfg := app.Config{
		Pool: pool, Clock: h.FakeClock, Rand: rand.New(rand.NewPCG(1, 2)), Games: catalog,
		Verifier: verifier, MatchTokenKey: bytes.Repeat([]byte("k"), 32), GameServers: h.FakeServers,
		Kubernetes: h.FakeKubernetes, ManifestLogger: slog.New(slog.NewTextHandler(io.Discard, nil)),
	}
	for _, o := range opts {
		o(&cfg)
	}
	h.App, err = app.New(cfg)
	if err != nil {
		t.Fatal(err)
	}
	h.App.Hub.Poll = 50 * time.Millisecond
	// One job at a time keeps every scenario repeatable.
	h.App.Scheduler.Concurrency = 1
	done := make(chan struct{})
	go func() {
		defer close(done)
		h.App.Hub.Run(ctx)
	}()
	srv := httptest.NewServer(h.App.Handler)
	h.URL = srv.URL
	t.Cleanup(func() {
		srv.CloseClientConnections()
		srv.Close()
		cancel()
		<-done
		pool.Close()
		issuer.Close()
	})
	return h
}

// Settle reconciles every Tournament Manifest and Recurring Tournament, then runs every due job and
// reconciles Game Servers until nothing changes, the way the background
// workers would.
func (h *Harness) Settle() {
	h.t.Helper()
	ctx := context.Background()
	h.reconcileManifests(ctx)
	for range 50 {
		ran, err := h.App.Scheduler.RunDue(ctx)
		if err != nil {
			h.t.Fatalf("run due jobs: %v", err)
		}
		if err := h.App.Service.Reconcile(ctx); err != nil {
			h.t.Fatalf("reconcile: %v", err)
		}
		// Reconciling can schedule work, e.g. reallocating an Aborted Match.
		again, err := h.App.Scheduler.RunDue(ctx)
		if err != nil {
			h.t.Fatalf("run due jobs: %v", err)
		}
		if ran+again == 0 {
			return
		}
	}
	h.t.Fatal("background work did not settle")
}

// reconcileManifests reconciles every Tournament Manifest and Recurring
// Tournament in the fake cluster, in every namespace, as the informers would
// deliver them. The app itself ignores the namespaces it doesn't watch.
func (h *Harness) reconcileManifests(ctx context.Context) {
	h.t.Helper()
	if h.App.Manifests == nil {
		return
	}
	list, err := h.FakeKubernetes.Resource(manifests.Resource).List(ctx, metav1.ListOptions{})
	if err != nil {
		h.t.Fatalf("list manifests: %v", err)
	}
	for _, u := range list.Items {
		name := cache.ObjectName{Namespace: u.GetNamespace(), Name: u.GetName()}
		if err := h.App.Manifests.Reconcile(ctx, name); err != nil {
			h.t.Fatalf("reconcile manifest %v: %v", name, err)
		}
	}
	list, err = h.FakeKubernetes.Resource(manifests.RecurringResource).List(ctx, metav1.ListOptions{})
	if err != nil {
		h.t.Fatalf("list recurring tournaments: %v", err)
	}
	for _, u := range list.Items {
		name := cache.ObjectName{Namespace: u.GetNamespace(), Name: u.GetName()}
		if _, err := h.App.Manifests.ReconcileRecurring(ctx, name); err != nil {
			h.t.Fatalf("reconcile recurring tournament %v: %v", name, err)
		}
	}
}

// Advance moves the clock forward and settles.
func (h *Harness) Advance(d time.Duration) {
	h.t.Helper()
	h.FakeClock.Advance(d)
	h.Settle()
}

// AdvanceTo moves the clock to t and settles.
func (h *Harness) AdvanceTo(t time.Time) {
	h.t.Helper()
	h.FakeClock.Set(t)
	h.Settle()
}

// Response is an HTTP response with its body read.
type Response struct {
	t      testing.TB
	Status int
	Body   []byte
	Header http.Header
}

// Decode unmarshals the body.
func (r *Response) Decode(v any) {
	r.t.Helper()
	if err := json.Unmarshal(r.Body, v); err != nil {
		r.t.Fatalf("decode %s: %v", r.Body, err)
	}
}

// Code is the Problem Details error code of a failed response.
func (r *Response) Code() string {
	r.t.Helper()
	var p struct{ Code string }
	if err := json.Unmarshal(r.Body, &p); err != nil {
		r.t.Fatalf("response %s is not Problem Details: %v", r.Body, err)
	}
	return p.Code
}

// Expect fails the test unless the response has the given status.
//
// It deviates from the coding standards' advice against assertion helpers
// (§11.1) on purpose: in these scenarios the status is a precondition of each
// step, like a setup helper's, and the message names the call's status and
// body. The behaviour under test is still checked in the Test functions.
func (r *Response) Expect(status int) *Response {
	r.t.Helper()
	if r.Status != status {
		r.t.Fatalf("status %d, want %d: %s", r.Status, status, r.Body)
	}
	return r
}

// Do sends a request with an optional bearer token and JSON body.
func (h *Harness) Do(method, path, token string, body any) *Response {
	h.t.Helper()
	var rd io.Reader
	if body != nil {
		b, err := json.Marshal(body)
		if err != nil {
			h.t.Fatal(err)
		}
		rd = bytes.NewReader(b)
	}
	req, err := http.NewRequest(method, h.URL+path, rd)
	if err != nil {
		h.t.Fatal(err)
	}
	if body != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	if token != "" {
		req.Header.Set("Authorization", "Bearer "+token)
	}
	res, err := http.DefaultClient.Do(req)
	if err != nil {
		h.t.Fatal(err)
	}
	defer res.Body.Close()
	b, err := io.ReadAll(res.Body)
	if err != nil {
		h.t.Fatalf("read %s %s: %v", method, path, err)
	}
	return &Response{t: h.t, Status: res.StatusCode, Body: b, Header: res.Header}
}

// User returns a Keycloak token for a human user.
func (h *Harness) User(subject string, extra ...map[string]any) string {
	return h.Issuer.User(subject, extra...)
}

// Client returns a Keycloak service-account token for a Game backend.
func (h *Harness) Client(clientID string) string { return h.Issuer.Client(clientID) }

// ServerToken returns the Match token the Game Server of a Match received.
func (h *Harness) ServerToken(match string) string {
	h.t.Helper()
	id, err := ids.Parse[ids.MatchID](match)
	if err != nil {
		h.t.Fatal(err)
	}
	_, token, ok := h.FakeServers.ServerFor(id)
	if !ok {
		h.t.Fatalf("match %s has no game server", match)
	}
	return token
}

// Path joins path segments.
func Path(parts ...string) string { return strings.Join(parts, "/") }
