package app_test

import (
	"encoding/json"
	"net/http"
	"testing"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

type problemDetails struct {
	Status int    `json:"status"`
	Code   string `json:"code"`
	Errors []struct {
		Location string `json:"location"`
		Message  string `json:"message"`
	} `json:"errors"`
}

func locations(r *apptest.Response) []string {
	var p problemDetails
	json.Unmarshal(r.Body, &p)
	var out []string
	for _, e := range p.Errors {
		out = append(out, e.Location)
	}
	return out
}

func TestOrganizerCreatesADraftTournament(t *testing.T) {
	h := apptest.MustStart(t)

	tt := mustCreate(t, h, settings(h))

	tv := tt.get()
	if tv.Status != "draft" || tv.Organizer != "user:organizer" {
		t.Fatalf("got %+v, want a draft organized by user:organizer", tv)
	}
}

func TestCreatingRequiresAToken(t *testing.T) {
	h := apptest.MustStart(t)

	r := h.Do(http.MethodPost, "/api/v1/tournaments", "", settings(h)).Expect(http.StatusUnauthorized)

	if r.Code() != "unauthenticated" || r.Header.Get("Content-Type") != "application/problem+json" {
		t.Fatalf("want an unauthenticated problem, got %s (%s)", r.Body, r.Header.Get("Content-Type"))
	}
}

func TestTrustedGameBackendCreatesTournamentsForItsGameOnly(t *testing.T) {
	h := apptest.MustStart(t)
	backend := h.Client("arena-backend")

	var tv tournamentView
	h.Do(http.MethodPost, "/api/v1/tournaments", backend, settings(h)).Expect(http.StatusCreated).Decode(&tv)
	if tv.Organizer != "client:arena-backend" {
		t.Fatalf("organizer = %s, want the backend", tv.Organizer)
	}

	royale := settings(h, stage("free-for-all", map[string]any{"bouts": 1}))
	royale["gameId"] = "royale"
	r := h.Do(http.MethodPost, "/api/v1/tournaments", backend, royale).Expect(http.StatusForbidden)
	if r.Code() != "not-trusted-for-game" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestImpossibleConfigurationsAreRejectedAllAtOnce(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h,
		stage("swiss", map[string]any{"groups": 4, "advancement": 2}),
		stage("free-for-all", map[string]any{"bouts": 3, "bestOf": 3}),
	)
	body["gameId"] = "royale"
	body["capacity"] = 40
	body["minimumParticipants"] = 8

	r := h.Do(http.MethodPost, "/api/v1/tournaments", h.User("organizer"), body).Expect(http.StatusUnprocessableEntity)

	got := locations(r)
	want := map[string]bool{
		// 8 Participants in 4 Swiss Groups is 2 each, fewer than Advancement + 1.
		"body.stages[0].groups": true,
		// A free-for-all has no best-of.
		"body.stages[1].bestOf": true,
	}
	for _, l := range got {
		delete(want, l)
	}
	if len(want) > 0 || r.Code() != "validation-failed" {
		t.Fatalf("errors at %v (code %s), missing %v", got, r.Code(), want)
	}
}

func TestFreeForAllGroupMustFitTheGamesMaximumMatchSize(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 2}))
	body["gameId"] = "royale"
	body["capacity"] = 9 // royale holds 8 per Match

	r := h.Do(http.MethodPost, "/api/v1/tournaments", h.User("organizer"), body).Expect(http.StatusUnprocessableEntity)

	if got := locations(r); len(got) != 1 || got[0] != "body.stages[0].groups" {
		t.Fatalf("errors at %v", got)
	}
	body["stages"] = []map[string]any{stage("free-for-all", map[string]any{"bouts": 2, "groups": 2})}
	body["minimumParticipants"] = 4
	h.Do(http.MethodPost, "/api/v1/tournaments", h.User("organizer"), body).Expect(http.StatusCreated)
}

func TestAdvancementMustLeaveSomeoneBehindInEveryGroup(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h,
		stage("round-robin", map[string]any{"advancement": 4}),
		stage("single-elimination", nil),
	)
	body["minimumParticipants"] = 4

	r := h.Do(http.MethodPost, "/api/v1/tournaments", h.User("organizer"), body).Expect(http.StatusUnprocessableEntity)

	if got := locations(r); len(got) == 0 || got[0] != "body.stages[0].groups" {
		t.Fatalf("errors at %v", got)
	}
}

func TestUnknownGameIsRejected(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h)
	body["gameId"] = "chess"

	r := h.Do(http.MethodPost, "/api/v1/tournaments", h.User("organizer"), body).Expect(http.StatusUnprocessableEntity)

	if got := locations(r); len(got) != 1 || got[0] != "body.gameId" {
		t.Fatalf("errors at %v", got)
	}
}

func TestOrganizerEditsADraftUntilRegistrationOpens(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	edit := settings(h)
	delete(edit, "gameId")
	edit["capacity"] = 4

	h.Do(http.MethodPut, tt.path(), h.User("someone-else"), edit).Expect(http.StatusForbidden)
	var tv struct{ Capacity int }
	h.Do(http.MethodPut, tt.path(), tt.Organizer, edit).Expect(http.StatusOK).Decode(&tv)
	if tv.Capacity != 4 {
		t.Fatalf("capacity = %d, want 4", tv.Capacity)
	}

	tt.openRegistration()

	r := h.Do(http.MethodPut, tt.path(), tt.Organizer, edit).Expect(http.StatusConflict)
	if r.Code() != "settings-frozen" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestEditingMovesTheRegistrationWindow(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	edit := settings(h)
	delete(edit, "gameId")
	edit["registrationOpensAt"] = h.FakeClock.Now().Add(45 * 60e9)
	h.Do(http.MethodPut, tt.path(), tt.Organizer, edit).Expect(http.StatusOK)

	tt.openRegistration() // 30 minutes in

	if got := tt.get().Status; got != "draft" {
		t.Fatalf("status = %s, want still draft", got)
	}
	h.Advance(15 * 60e9)
	if got := tt.get().Status; got != "registration-open" {
		t.Fatalf("status = %s, want registration-open", got)
	}
}

func TestListsTournamentsByStatus(t *testing.T) {
	h := apptest.MustStart(t)
	a := mustCreate(t, h, settings(h))
	mustCreate(t, h, settings(h))
	a.openRegistration()
	b := mustCreate(t, h, settings(h)) // created after, still a draft

	var body struct {
		Tournaments []tournamentView `json:"tournaments"`
	}
	h.Do(http.MethodGet, "/api/v1/tournaments?status=draft", "", nil).Expect(http.StatusOK).Decode(&body)

	if len(body.Tournaments) != 1 || body.Tournaments[0].ID != b.ID {
		t.Fatalf("drafts = %+v, want only %s", body.Tournaments, b.ID)
	}
}
