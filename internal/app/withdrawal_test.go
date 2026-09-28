package app_test

import (
	"net/http"
	"slices"
	"testing"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
	"github.com/couchpartygames/opentournament/internal/ids"
)

func (tt *tournament) withdraw(p string) *apptest.Response {
	return tt.Do(http.MethodPost, tt.path("participants", p, "withdraw"), tt.Players[p], nil)
}

func TestWithdrawalMidMatchForfeitsEveryRemainingBout(t *testing.T) {
	tt := running(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]
	token := tt.server(final.ID)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/started", token, nil).Expect(http.StatusNoContent)
	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)

	tt.withdraw(a).Expect(http.StatusNoContent)

	m := tt.match(final.ID)
	if m.Status != "completed" || m.WinnerID != b || len(m.Bouts) != 3 {
		t.Fatalf("match = %+v, want %s to win 2–1", m, b)
	}
	if tt.Servers.Running() != 0 {
		t.Fatal("server should be released")
	}
}

func TestLateResultForAForfeitedBoutIsRejected(t *testing.T) {
	tt := running(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]
	token := tt.server(final.ID)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/1/no-shows", token,
		map[string]any{"participantIds": []string{a}}).Expect(http.StatusNoContent)

	r := tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusConflict)

	if r.Code() != "bout-already-forfeited" {
		t.Fatalf("code = %s", r.Code())
	}
	if m := tt.match(final.ID); m.Bouts[0].Results[0].Won != (m.Bouts[0].Results[0].ParticipantID == b) {
		t.Fatalf("bout 1 = %+v, want won by %s", m.Bouts[0], b)
	}
}

func TestOnlyTheParticipantCanWithdrawAndOnlyTheOrganizerCanDisqualify(t *testing.T) {
	tt := running(t, 4)
	ps := make([]string, 0, 4)
	for p := range tt.Players {
		ps = append(ps, p)
	}

	tt.Do(http.MethodPost, tt.path("participants", ps[0], "withdraw"), tt.Players[ps[1]], nil).Expect(http.StatusForbidden)
	tt.Do(http.MethodPost, tt.path("participants", ps[0], "disqualify"), tt.Players[ps[1]], nil).Expect(http.StatusForbidden)
	tt.Do(http.MethodPost, tt.path("participants", ps[0], "disqualify"), tt.Organizer, nil).Expect(http.StatusNoContent)

	r := tt.Do(http.MethodPost, tt.path("participants", ps[0], "disqualify"), tt.Organizer, nil).Expect(http.StatusConflict)
	if r.Code() != "participant-not-active" {
		t.Fatalf("code = %s", r.Code())
	}
}

func TestWithdrawnParticipantForfeitsTheirNextMatchWhenItBecomesReady(t *testing.T) {
	tt := running(t, 4)
	semis := tt.matches("allocating")
	winner := semis[0].Participants[0]
	tt.playBout(semis[0], 1, winner)
	tt.withdraw(winner).Expect(http.StatusNoContent)

	tt.playBout(semis[1], 1, semis[1].Participants[0])

	if tt.get().Status != "completed" {
		t.Fatalf("tournament = %s, want completed without playing the final", tt.get().Status)
	}
	if got := tt.placementOf(tt.Names[winner]); got != [2]int{2, 2} {
		t.Fatalf("withdrawn finalist placed %v, want 2nd", got)
	}
	// The final was forfeited the moment it became Ready, so it never needed a server.
	if n := len(tt.Servers.Allocations()); n != 2 {
		t.Fatalf("%d servers allocated, want 2", n)
	}
}

func TestWithdrawalFromAFreeForAllMatchTellsTheGameServer(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 2}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := create(t, h, body)
	tt.openRegistration()
	ps := tt.register("anna", "bert", "cleo")
	tt.start()
	m := tt.matches("allocating")[0]
	token := tt.server(m.ID)
	tt.reportBout(token, 1, map[string]any{"placements": []map[string]any{
		{"participantId": ps[0], "placement": 1, "points": 10},
		{"participantId": ps[1], "placement": 2, "points": 5},
		{"participantId": ps[2], "placement": 3, "points": 1},
	}}).Expect(http.StatusNoContent)

	tt.withdraw(ps[0]).Expect(http.StatusNoContent)

	id, _ := ids.Parse[ids.MatchID](m.ID)
	server, _, _ := tt.Servers.ServerFor(id)
	pid, _ := ids.Parse[ids.ParticipantID](ps[0])
	if got := tt.Servers.Forfeits(server.Name); !slices.Contains(got, pid) {
		t.Fatalf("server told %v, want anna", got)
	}
	var sm struct {
		Participants []struct {
			ParticipantID string `json:"participantId"`
			Forfeited     bool   `json:"forfeited"`
		} `json:"participants"`
	}
	tt.Do(http.MethodGet, "/api/v1/game-server/match", token, nil).Expect(http.StatusOK).Decode(&sm)
	for _, p := range sm.Participants {
		if p.Forfeited != (p.ParticipantID == ps[0]) {
			t.Fatalf("participants = %+v, want only anna forfeited", sm.Participants)
		}
	}
	r := tt.reportBout(token, 2, map[string]any{"placements": []map[string]any{
		{"participantId": ps[0], "placement": 1, "points": 10},
		{"participantId": ps[1], "placement": 2, "points": 5},
		{"participantId": ps[2], "placement": 3, "points": 1},
	}}).Expect(http.StatusConflict)
	if r.Code() != "bout-already-forfeited" {
		t.Fatalf("code = %s", r.Code())
	}
	tt.reportBout(token, 2, map[string]any{"placements": []map[string]any{
		{"participantId": ps[2], "placement": 1, "points": 10},
		{"participantId": ps[1], "placement": 2, "points": 5},
	}}).Expect(http.StatusNoContent)

	if got := tt.placementOf("cleo"); got != [2]int{1, 1} {
		t.Fatalf("cleo placed %v, want 1st on 11 points", got)
	}
	// anna's forfeited bout scored nothing, leaving her level with bert on 10
	// points; her 1st place in bout 1 is the better single placement.
	if got := tt.placementOf("anna"); got != [2]int{2, 2} {
		t.Fatalf("anna placed %v, want 2nd", got)
	}
	if got := tt.placementOf("bert"); got != [2]int{3, 3} {
		t.Fatalf("bert placed %v, want 3rd", got)
	}
}

func TestFreeForAllCompletesAtOnceWhenOnlyOneParticipantIsLeft(t *testing.T) {
	h := apptest.Start(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 3}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := create(t, h, body)
	tt.openRegistration()
	ps := tt.register("anna", "bert", "cleo")
	tt.start()

	tt.withdraw(ps[0]).Expect(http.StatusNoContent)
	tt.Do(http.MethodPost, tt.path("participants", ps[1], "disqualify"), tt.Organizer, nil).Expect(http.StatusNoContent)

	if tt.get().Status != "completed" || tt.Servers.Running() != 0 {
		t.Fatalf("tournament = %s with %d servers, want completed and released", tt.get().Status, tt.Servers.Running())
	}
}

func TestSwissLeavesWithdrawnParticipantsOutOfLaterRounds(t *testing.T) {
	tt := running(t, 4, stage("swiss", nil))
	round1 := tt.matches("allocating")
	gone := round1[0].Participants[1]
	tt.playBout(round1[0], 1, round1[0].Participants[0])
	tt.withdraw(gone).Expect(http.StatusNoContent)

	tt.playBout(round1[1], 1, round1[1].Participants[0])

	for _, m := range tt.matches("") {
		if m.Round == 2 && slices.Contains(m.Participants, gone) {
			t.Fatalf("withdrawn participant paired in round 2: %+v", m)
		}
	}
}

func TestOrganizerCancelsARunningTournament(t *testing.T) {
	tt := running(t, 4)

	tt.Do(http.MethodPost, tt.path("cancel"), tt.Players[tt.matches("allocating")[0].Participants[0]], nil).Expect(http.StatusForbidden)
	tt.Do(http.MethodPost, tt.path("cancel"), tt.Organizer, nil).Expect(http.StatusOK)

	if tt.get().Status != "cancelled" || tt.Servers.Running() != 0 {
		t.Fatalf("status %s with %d servers", tt.get().Status, tt.Servers.Running())
	}
	for _, m := range tt.matches("") {
		if m.Status != "cancelled" {
			t.Fatalf("match %s is %s, want cancelled", m.Key, m.Status)
		}
	}
	if r := tt.Do(http.MethodPost, tt.path("cancel"), tt.Organizer, nil).Expect(http.StatusConflict); r.Code() != "wrong-status" {
		t.Fatalf("code = %s", r.Code())
	}
}
