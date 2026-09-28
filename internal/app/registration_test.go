package app_test

import (
	"net/http"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

func TestRegistrationIsRefusedOutsideTheRegistrationWindow(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, settings(h))

	r := h.Do(http.MethodPost, tt.path("participants"), h.User("anna"), map[string]any{}).Expect(http.StatusConflict)
	if r.Code() != "registration-closed" {
		t.Fatalf("code = %s", r.Code())
	}

	tt.openRegistration()
	tt.register("anna", "bert")
	tt.start()

	r = h.Do(http.MethodPost, tt.path("participants"), h.User("cleo"), map[string]any{}).Expect(http.StatusConflict)
	if r.Code() != "registration-closed" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestRegistrationIsFirstComeFirstServedUpToCapacity(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h)
	body["capacity"] = 2
	tt := create(t, h, body)
	tt.openRegistration()
	tt.register("anna", "bert")

	r := h.Do(http.MethodPost, tt.path("participants"), h.User("cleo"), map[string]any{}).Expect(http.StatusConflict)

	if r.Code() != "tournament-full" {
		t.Fatalf("code = %s", r.Code())
	}
	if got := tt.get().Registered; got != 2 {
		t.Fatalf("registered = %d, want 2", got)
	}
}

func TestAPlayerCannotRegisterTwice(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, settings(h))
	tt.openRegistration()
	tt.register("anna")

	r := h.Do(http.MethodPost, tt.path("participants"), h.User("anna"), map[string]any{}).Expect(http.StatusConflict)

	if r.Code() != "already-registered" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestUnregisteringFreesTheSpot(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h)
	body["capacity"] = 2
	tt := create(t, h, body)
	tt.openRegistration()
	ps := tt.register("anna", "bert")

	h.Do(http.MethodDelete, tt.path("participants", ps[0]), h.User("bert"), nil).Expect(http.StatusForbidden)
	h.Do(http.MethodDelete, tt.path("participants", ps[0]), h.User("anna"), nil).Expect(http.StatusNoContent)

	tt.register("cleo")
}

func TestGameBackendRegistersAnyAcceptedPlayerIdentity(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, settings(h))
	tt.openRegistration()
	backend := h.Client("arena-backend")
	steam := map[string]any{"identity": map[string]any{"kind": "steam", "value": "76561198000000001"}}

	var p participantView
	h.Do(http.MethodPost, tt.path("participants"), backend, steam).Expect(http.StatusCreated).Decode(&p)
	if p.Identity.Kind != "steam" || p.Status != "registered" {
		t.Fatalf("participant = %+v", p)
	}

	// An untrusted backend can't, and neither can a player for someone else.
	h.Do(http.MethodPost, tt.path("participants"), h.Client("royale-backend"), steam).Expect(http.StatusForbidden)
	other := map[string]any{"identity": map[string]any{"kind": "steam", "value": "76561198000000002"}}
	r := h.Do(http.MethodPost, tt.path("participants"), h.User("anna"), other).Expect(http.StatusForbidden)
	if r.Code() != "identity-not-owned" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestPlayerRegistersTheirLinkedIdentityFromAClaim(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, settings(h))
	tt.openRegistration()
	anna := h.User("anna", map[string]any{"steam_id": "76561198000000009"})

	h.Do(http.MethodPost, tt.path("participants"), anna,
		map[string]any{"identity": map[string]any{"kind": "steam", "value": "76561198000000009"}}).Expect(http.StatusCreated)

	var mine struct{ Participants []participantView }
	h.Do(http.MethodGet, tt.path("participants", "me"), anna, nil).Expect(http.StatusOK).Decode(&mine)
	if len(mine.Participants) != 1 || mine.Participants[0].Identity.Kind != "steam" {
		t.Fatalf("my registrations = %+v", mine.Participants)
	}
}

func TestIdentityKindsTheGameDoesNotAcceptAreRefused(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 1}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := create(t, h, body)
	tt.openRegistration()

	r := h.Do(http.MethodPost, tt.path("participants"), h.Client("royale-backend"),
		map[string]any{"identity": map[string]any{"kind": "steam", "value": "1"}}).Expect(http.StatusUnprocessableEntity)

	if r.Code() != "identity-kind-not-accepted" {
		t.Fatalf("code = %s", r.Code())
	}
}

func checkInSettings(h *apptest.Harness) map[string]any {
	body := settings(h)
	body["checkIn"] = map[string]any{"enabled": true, "windowSeconds": 600}
	return body
}

func TestCheckInWindowOpensBeforeTheStartAndLateRegistrationsCountAsCheckedIn(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, checkInSettings(h))
	tt.openRegistration()
	ps := tt.register("anna", "bert")

	r := h.Do(http.MethodPost, tt.path("participants", ps[0], "check-in"), h.User("anna"), nil).Expect(http.StatusConflict)
	if r.Code() != "check-in-closed" {
		t.Fatalf("code = %s", r.Code())
	}

	h.AdvanceTo(apptest.Epoch.Add(startsIn - 10*time.Minute))
	if got := tt.get().Status; got != "check-in" {
		t.Fatalf("status = %s, want check-in", got)
	}
	var p participantView
	h.Do(http.MethodPost, tt.path("participants", ps[0], "check-in"), h.User("anna"), nil).Expect(http.StatusOK).Decode(&p)
	if p.Status != "checked-in" {
		t.Fatalf("anna = %+v", p)
	}
	late := tt.register("cleo")
	var mine struct{ Participants []participantView }
	h.Do(http.MethodGet, tt.path("participants", "me"), h.User("cleo"), nil).Expect(http.StatusOK).Decode(&mine)
	if mine.Participants[0].ID != late[0] || mine.Participants[0].Status != "checked-in" {
		t.Fatalf("cleo = %+v, want checked in on registration", mine.Participants)
	}
}

func TestParticipantsWhoDidNotCheckInAreDroppedAtTheStart(t *testing.T) {
	h := apptest.Start(t)
	tt := create(t, h, checkInSettings(h))
	tt.openRegistration()
	ps := tt.register("anna", "bert", "cleo")
	h.AdvanceTo(apptest.Epoch.Add(startsIn - 10*time.Minute))
	h.Do(http.MethodPost, tt.path("participants", ps[0], "check-in"), h.User("anna"), nil).Expect(http.StatusOK)
	// A Game backend can check a player in on their behalf.
	h.Do(http.MethodPost, tt.path("participants", ps[1], "check-in"), h.Client("arena-backend"), nil).Expect(http.StatusOK)

	tt.start()

	entrants := tt.structure().Stages[0].Groups[0].Entrants
	if len(entrants) != 2 {
		t.Fatalf("entrants = %+v, want only the two who checked in", entrants)
	}
	var list struct{ Participants []participantView }
	h.Do(http.MethodGet, tt.path("participants"), "", nil).Expect(http.StatusOK).Decode(&list)
	for _, p := range list.Participants {
		if p.ID == ps[2] && p.Status != "not-checked-in" {
			t.Fatalf("cleo = %+v, want not-checked-in", p)
		}
	}
}

func TestTournamentBelowMinimumParticipantsIsCancelledAtTheStart(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h)
	body["minimumParticipants"] = 3
	tt := create(t, h, body)
	tt.openRegistration()
	tt.register("anna", "bert")

	tt.start()

	if got := tt.get().Status; got != "cancelled" {
		t.Fatalf("status = %s, want cancelled", got)
	}
}
