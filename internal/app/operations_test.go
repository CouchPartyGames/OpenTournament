package app_test

import (
	"context"
	"encoding/json"
	"net/http"
	"slices"
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

func TestOpenAPIDocumentListsEveryTournamentStatus(t *testing.T) {
	h := apptest.MustStart(t)

	type schema struct {
		Enum []any `json:"enum"` // not all enums are strings, e.g. bestOf
	}
	var doc struct {
		Paths map[string]map[string]struct {
			Parameters []struct {
				Name   string `json:"name"`
				Schema schema `json:"schema"`
			} `json:"parameters"`
		} `json:"paths"`
		Components struct {
			Schemas map[string]struct {
				Properties map[string]schema `json:"properties"`
			} `json:"schemas"`
		} `json:"components"`
	}
	h.Do(http.MethodGet, "/api/v1/openapi.json", "", nil).Expect(http.StatusOK).Decode(&doc)

	got := map[string][]any{}
	for _, p := range doc.Paths["/api/v1/tournaments"]["get"].Parameters {
		if p.Name == "status" {
			got["list-tournaments ?status"] = p.Schema.Enum
		}
	}
	for _, name := range []string{"TournamentView", "StructureView", "PlacementsView"} {
		got[name+".status"] = doc.Components.Schemas[name].Properties["status"].Enum
	}

	want := []any{"draft", "registration-open", "check-in", "running", "completed", "cancelled"}
	if len(got) != 4 {
		t.Errorf("status enums found at %v, want the list filter and three views", got)
	}
	for where, enum := range got {
		if !slices.Equal(enum, want) {
			t.Errorf("%s enum = %v, want %v", where, enum, want)
		}
	}
}
