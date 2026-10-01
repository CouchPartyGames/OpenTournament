package app_test

import (
	"net/http"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

func TestGameServerFetchesItsMatch(t *testing.T) {
	tt := mustRun(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]

	var m struct {
		MatchID      string `json:"matchId"`
		GameID       string `json:"gameId"`
		BestOf       int    `json:"bestOf"`
		Participants []struct {
			ParticipantID string `json:"participantId"`
			IdentityKind  string `json:"identityKind"`
			IdentityValue string `json:"identityValue"`
			Forfeited     bool   `json:"forfeited"`
		} `json:"participants"`
	}
	tt.Do(http.MethodGet, "/api/v1/game-server/match", tt.server(final.ID), nil).Expect(http.StatusOK).Decode(&m)

	if m.MatchID != final.ID || m.GameID != "arena" || m.BestOf != 3 || len(m.Participants) != 2 {
		t.Fatalf("match = %+v", m)
	}
	if m.Participants[0].IdentityKind != "keycloak" || m.Participants[0].IdentityValue == "" {
		t.Fatalf("participants = %+v", m.Participants)
	}
}

func TestGameServerEndpointsOnlyAcceptMatchTokens(t *testing.T) {
	tt := mustRun(t, 2)

	for _, token := range []string{"", tt.Organizer, "not-a-token"} {
		r := tt.Do(http.MethodGet, "/api/v1/game-server/match", token, nil).Expect(http.StatusUnauthorized)
		if r.Code() != "match-token-invalid" {
			t.Fatalf("code = %s", r.Code())
		}
	}
}

func TestStartedMatchShowsInProgress(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]

	tt.Do(http.MethodPost, "/api/v1/game-server/match/started", tt.server(final.ID), nil).Expect(http.StatusNoContent)

	if got := tt.match(final.ID).Status; got != "in-progress" {
		t.Fatalf("status = %s, want in-progress", got)
	}
}

func TestBestOfThreeEndsAsSoonAsSomeoneWinsTwoBouts(t *testing.T) {
	tt := mustRun(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]
	token := tt.server(final.ID)

	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)
	tt.reportBout(token, 2, map[string]any{"winner": b}).Expect(http.StatusNoContent)
	if got := tt.match(final.ID).Status; got == "completed" {
		t.Fatal("completed at 1–1")
	}
	tt.reportBout(token, 3, map[string]any{"winner": b}).Expect(http.StatusNoContent)

	m := tt.match(final.ID)
	if m.Status != "completed" || m.WinnerID != b {
		t.Fatalf("match = %+v, want won by %s", m, b)
	}
	if tt.get().Status != "completed" {
		t.Fatal("tournament should be complete")
	}
}

func TestRepeatingABoutIsIdempotentButADifferentResultConflicts(t *testing.T) {
	tt := mustRun(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]
	token := tt.server(final.ID)
	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)

	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)
	if r := tt.reportBout(token, 1, map[string]any{"winner": b}).Expect(http.StatusConflict); r.Code() != "bout-conflict" {
		t.Fatalf("code = %s", r.Code())
	}
	if r := tt.reportBout(token, 3, map[string]any{"winner": b}).Expect(http.StatusConflict); r.Code() != "bout-out-of-order" {
		t.Fatalf("code = %s", r.Code())
	}
	if len(tt.match(final.ID).Bouts) != 1 {
		t.Fatal("want exactly one bout recorded")
	}
}

func TestTokenIsRejectedOnceTheMatchCompletes(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]
	token := tt.server(final.ID)
	tt.reportBout(token, 1, map[string]any{"winner": final.Participants[0]}).Expect(http.StatusNoContent)

	tt.Do(http.MethodGet, "/api/v1/game-server/match", token, nil).Expect(http.StatusUnauthorized)
	if tt.FakeServers.Running() != 0 {
		t.Fatal("the game server should be released once the match completes")
	}
}

func TestNoShowForfeitsTheBoutToTheOpponent(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]

	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/1/no-shows", tt.server(final.ID),
		map[string]any{"participantIds": []string{a}}).Expect(http.StatusNoContent)

	if m := tt.match(final.ID); m.WinnerID != b || !m.Bouts[0].Results[0].Forfeited && !m.Bouts[0].Results[1].Forfeited {
		t.Fatalf("match = %+v, want %s to win by forfeit", m, b)
	}
}

func TestDoubleNoShowGivesTheNextOpponentABye(t *testing.T) {
	tt := mustRun(t, 4)
	semis := tt.matches("allocating")
	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/1/no-shows", tt.server(semis[0].ID),
		map[string]any{"participantIds": semis[0].Participants}).Expect(http.StatusNoContent)
	tt.playBout(semis[1], 1, semis[1].Participants[0])

	if got := tt.match(semis[0].ID).Result; got != "double-forfeit" {
		t.Fatalf("result = %s, want double-forfeit", got)
	}
	var final matchView
	for _, m := range tt.matches("") {
		if m.Key == "R2-M1" {
			final = m
		}
	}
	if final.Result != "bye" || final.WinnerID != semis[1].Participants[0] {
		t.Fatalf("final = %+v, want a bye for the other semifinal's winner", final)
	}
	if tt.get().Status != "completed" {
		t.Fatal("tournament should be complete")
	}
	if got := tt.placementOf(tt.Names[semis[0].Participants[0]]); got != [2]int{2, 4} {
		t.Fatalf("double forfeiter placed %v, want 2nd–4th with the other round 1 loser", got)
	}
}

func TestGameServerAddressIsOnlyShownToTheMatchesParticipantsAndOrganizer(t *testing.T) {
	tt := mustRun(t, 4)
	semi := tt.matches("allocating")[0]
	var other string
	for id := range tt.Tokens {
		if id != semi.Participants[0] && id != semi.Participants[1] {
			other = id
		}
	}

	for who, token := range map[string]string{"participant": tt.Tokens[semi.Participants[0]], "organizer": tt.Organizer} {
		var m matchView
		tt.Do(http.MethodGet, "/api/v1/matches/"+semi.ID, token, nil).Expect(http.StatusOK).Decode(&m)
		if m.ServerAddress == "" || m.ServerPort == 0 {
			t.Fatalf("%s can't see the server: %+v", who, m)
		}
	}
	for who, token := range map[string]string{"anonymous": "", "another Participant": tt.Tokens[other]} {
		var m matchView
		tt.Do(http.MethodGet, "/api/v1/matches/"+semi.ID, token, nil).Expect(http.StatusOK).Decode(&m)
		if m.ServerAddress != "" || !m.ServerAllocated {
			t.Fatalf("%s sees %+v, want only that a server is allocated", who, m)
		}
	}
}

func TestAllocationIsRetriedWithBackoffWhenNoServerIsAvailable(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	tt.openRegistration()
	tt.mustRegister("anna", "bert")
	h.FakeServers.FailAllocations(3)

	tt.start()

	final := tt.matches("")[0]
	if final.Status != "allocating" || final.ServerAllocated {
		t.Fatalf("final = %+v, want waiting for a server", final)
	}
	// After 1s, 2s and 4s of backoff a server is free.
	h.Advance(time.Second)
	h.Advance(2 * time.Second)
	if tt.match(final.ID).ServerAllocated {
		t.Fatal("allocated before the third retry")
	}
	h.Advance(4 * time.Second)
	if !tt.match(final.ID).ServerAllocated {
		t.Fatal("want a server after the retries")
	}
}

func TestEveryFirstRoundMatchGetsOneServerInTheStartBurst(t *testing.T) {
	h := apptest.MustStart(t)
	h.App.Scheduler.Concurrency = 16
	body := settings(h, stage("single-elimination", nil))
	body["capacity"] = 64
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	tt.mustRegisterN(64)

	tt.start()

	first := tt.matches("allocating")
	if len(first) != 32 {
		t.Fatalf("%d matches allocating, want all 32 of round 1", len(first))
	}
	perMatch := map[string]int{}
	for _, a := range h.FakeServers.Allocations() {
		perMatch[a.MatchID.String()]++
	}
	for _, m := range first {
		if perMatch[m.ID] != 1 || !m.ServerAllocated {
			t.Fatalf("match %s got %d servers", m.Key, perMatch[m.ID])
		}
	}
}

func TestRepeatedNoShowIDCountsOnce(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]

	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/1/no-shows", tt.server(final.ID),
		map[string]any{"participantIds": []string{a, a}}).Expect(http.StatusUnprocessableEntity)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/1/no-shows", tt.server(final.ID),
		map[string]any{"participantIds": []string{a}}).Expect(http.StatusNoContent)

	if m := tt.match(final.ID); m.Status != "completed" || m.WinnerID != b {
		t.Fatalf("match = %+v, want won by %s", m, b)
	}
}

func TestDecidingBoutCanBeRetriedUntilTheMatchTokenExpires(t *testing.T) {
	tt := mustRun(t, 2)
	final := tt.matches("allocating")[0]
	a, b := final.Participants[0], final.Participants[1]
	token := tt.server(final.ID)

	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)
	tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)
	if r := tt.reportBout(token, 1, map[string]any{"winner": b}).Expect(http.StatusConflict); r.Code() != "bout-conflict" {
		t.Fatalf("different deciding Bout code = %s, want bout-conflict", r.Code())
	}
	tt.Do(http.MethodGet, "/api/v1/game-server/match", token, nil).Expect(http.StatusUnauthorized)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/started", token, nil).Expect(http.StatusUnauthorized)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/bouts/2/no-shows", token,
		map[string]any{"participantIds": []string{b}}).Expect(http.StatusUnauthorized)
	if r := tt.reportBout(token, 2, map[string]any{"winner": a}).Expect(http.StatusConflict); r.Code() != "wrong-status" {
		t.Fatalf("new Bout code = %s, want wrong-status", r.Code())
	}
	if got := tt.match(final.ID); got.Status != "completed" || len(got.Bouts) != 1 || got.WinnerID != a {
		t.Fatalf("Match after retries = %+v, want completed with one Bout won by %s", got, a)
	}
	tt.Advance(13 * time.Minute) // Result Deadline is 10 minutes, with a 2-minute token grace period.
	if r := tt.reportBout(token, 1, map[string]any{"winner": a}).Expect(http.StatusUnauthorized); r.Code() != "match-token-invalid" {
		t.Fatalf("expired token code = %s, want match-token-invalid", r.Code())
	}
}

func TestFinalFreeForAllPlacementsCanBeRetriedButNotChanged(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 1}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	ps := tt.mustRegister("anna", "bert", "cleo")
	tt.start()
	m := tt.matches("allocating")[0]
	token := tt.server(m.ID)
	report := map[string]any{"placements": []map[string]any{
		{"participantId": ps[0], "placement": 1, "points": 10},
		{"participantId": ps[1], "placement": 2, "points": 5},
		{"participantId": ps[2], "placement": 3, "points": 1},
	}}
	tt.reportBout(token, 1, report).Expect(http.StatusNoContent)
	tt.reportBout(token, 1, report).Expect(http.StatusNoContent)
	report["placements"].([]map[string]any)[0]["points"] = 20
	if r := tt.reportBout(token, 1, report).Expect(http.StatusConflict); r.Code() != "bout-conflict" {
		t.Fatalf("changed final placements code = %s, want bout-conflict", r.Code())
	}
	if got := tt.match(m.ID); got.Status != "completed" || len(got.Bouts) != 1 {
		t.Fatalf("Match after retries = %+v, want completed with one Bout", got)
	}
}

func TestCompletedFreeForAllRejectsNewResultsInPartiallyForfeitedBouts(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h, stage("free-for-all", map[string]any{"bouts": 2}))
	body["gameId"] = "royale"
	body["capacity"] = 8
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	ps := tt.mustRegister("anna", "bert", "cleo")
	tt.start()
	m := tt.matches("allocating")[0]
	token := tt.server(m.ID)
	tt.withdraw(ps[0]).Expect(http.StatusNoContent)
	tt.withdraw(ps[1]).Expect(http.StatusNoContent)
	if got := tt.match(m.ID).Status; got != "completed" {
		t.Fatalf("Match after withdrawals = %s, want completed", got)
	}
	r := tt.reportBout(token, 1, map[string]any{"placements": []map[string]any{
		{"participantId": ps[2], "placement": 1, "points": 10},
	}}).Expect(http.StatusConflict)
	if r.Code() != "wrong-status" {
		t.Fatalf("new result in partially forfeited Bout code = %s, want wrong-status", r.Code())
	}
	for _, bout := range tt.match(m.ID).Bouts {
		for _, result := range bout.Results {
			if result.ParticipantID == ps[2] {
				t.Fatalf("completed Match gained a result for the remaining Participant: %+v", result)
			}
		}
	}
}
