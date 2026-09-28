package app_test

import (
	"context"
	"encoding/json"
	"net/http"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

func TestHealthChecksFollowStartupAndDraining(t *testing.T) {
	h := apptest.MustStart(t)
	h.Do(http.MethodGet, "/livez", "", nil).Expect(http.StatusOK)
	h.Do(http.MethodGet, "/startupz", "", nil).Expect(http.StatusServiceUnavailable)

	ctx, cancel := context.WithCancel(context.Background())
	done := make(chan struct{})
	go func() { h.App.Run(ctx); close(done) }()
	t.Cleanup(func() { cancel(); <-done })

	deadline := time.Now().Add(5 * time.Second)
	for h.Do(http.MethodGet, "/startupz", "", nil).Status != http.StatusOK {
		if time.Now().After(deadline) {
			t.Fatal("never started")
		}
		time.Sleep(10 * time.Millisecond)
	}
	h.Do(http.MethodGet, "/readyz", "", nil).Expect(http.StatusOK)

	h.App.Drain()

	h.Do(http.MethodGet, "/readyz", "", nil).Expect(http.StatusServiceUnavailable)
	h.Do(http.MethodGet, "/livez", "", nil).Expect(http.StatusOK)
}

func TestOpenAPIDocumentDescribesTheVersionedAPI(t *testing.T) {
	h := apptest.MustStart(t)

	var doc struct {
		OpenAPI string                     `json:"openapi"`
		Paths   map[string]json.RawMessage `json:"paths"`
	}
	h.Do(http.MethodGet, "/api/v1/openapi.json", "", nil).Expect(http.StatusOK).Decode(&doc)

	if doc.OpenAPI != "3.1.0" {
		t.Fatalf("openapi = %s", doc.OpenAPI)
	}
	for _, p := range []string{
		"/api/v1/tournaments", "/api/v1/tournaments/{tournamentId}/participants",
		"/api/v1/game-server/match/bouts/{bout}", "/api/v1/matches/{matchId}/resolve",
	} {
		if _, ok := doc.Paths[p]; !ok {
			t.Fatalf("path %s missing from %v", p, doc.Paths)
		}
	}
}
