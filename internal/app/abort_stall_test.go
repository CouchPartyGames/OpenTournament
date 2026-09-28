package app_test

import (
	"net/http"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/ids"
)

func serverName(tt *tournament, match string) string {
	tt.t.Helper()
	id, _ := ids.Parse[ids.MatchID](match)
	s, _, ok := tt.Servers.ServerFor(id)
	if !ok {
		tt.t.Fatalf("match %s has no server", match)
	}
	return s.Name
}

func TestCrashedGameServerAbortsTheMatchAndKeepsCompletedBouts(t *testing.T) {
	tt := running(t, 2, stage("single-elimination", map[string]any{"bestOf": 3}))
	final := tt.matches("allocating")[0]
	a := final.Participants[0]
	oldToken := tt.server(final.ID)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/started", oldToken, nil).Expect(http.StatusNoContent)
	tt.reportBout(oldToken, 1, map[string]any{"winner": a}).Expect(http.StatusNoContent)

	tt.Servers.MakeUnhealthy(serverName(tt, final.ID))
	tt.Settle()

	m := tt.match(final.ID)
	if m.Aborts != 1 || !m.ServerAllocated || m.Status != "allocating" {
		t.Fatalf("match = %+v, want aborted once and on a new server", m)
	}
	if len(m.Bouts) != 1 || m.Bouts[0].Results[0].Won != (m.Bouts[0].Results[0].ParticipantID == a) {
		t.Fatalf("bouts = %+v, want the completed bout kept", m.Bouts)
	}
	// The old server's token no longer works; the new one resumes at bout 2.
	tt.Do(http.MethodGet, "/api/v1/game-server/match", oldToken, nil).Expect(http.StatusUnauthorized)
	newToken := tt.server(final.ID)
	var sm struct {
		CompletedBouts []boutView `json:"completedBouts"`
	}
	tt.Do(http.MethodGet, "/api/v1/game-server/match", newToken, nil).Expect(http.StatusOK).Decode(&sm)
	if len(sm.CompletedBouts) != 1 {
		t.Fatalf("new server sees %d completed bouts, want 1", len(sm.CompletedBouts))
	}
	tt.reportBout(newToken, 2, map[string]any{"winner": a}).Expect(http.StatusNoContent)
	if got := tt.match(final.ID); got.Status != "completed" || got.WinnerID != a {
		t.Fatalf("match = %+v", got)
	}
}

func TestServerThatVanishedWhileTheServiceWasDownIsDetectedByReconciliation(t *testing.T) {
	tt := running(t, 2)
	final := tt.matches("allocating")[0]

	tt.Servers.Delete(serverName(tt, final.ID), true) // no event: the service was down
	tt.Settle()                                       // startup reconciliation

	if m := tt.match(final.ID); m.Aborts != 1 || !m.ServerAllocated {
		t.Fatalf("match = %+v, want aborted and reallocated", m)
	}
}

func TestLeakedServerIsReleased(t *testing.T) {
	tt := running(t, 2)
	leaked := tt.Servers.Leak(ids.New[ids.MatchID]())

	tt.Settle()

	for _, r := range tt.Servers.Released() {
		if r == leaked {
			return
		}
	}
	t.Fatalf("leaked server %s not released; released %v", leaked, tt.Servers.Released())
}

func TestMatchWithoutAResultByItsDeadlineIsStalledAndAnAbortDoesNotResetIt(t *testing.T) {
	tt := running(t, 2) // Result Deadline: 10 minutes
	final := tt.matches("allocating")[0]
	tt.Advance(6 * time.Minute)
	tt.Servers.MakeUnhealthy(serverName(tt, final.ID))
	tt.Settle()

	tt.Advance(4 * time.Minute)

	if got := tt.match(final.ID); got.Status != "stalled" || got.ServerAllocated {
		t.Fatalf("match = %+v, want stalled with its server released", got)
	}
	if tt.Servers.Running() != 0 {
		t.Fatal("stalled match's server should be released")
	}
}

func TestOrganizerResolvesAStalledMatch(t *testing.T) {
	tt := running(t, 4)
	semi := tt.matches("allocating")[0]
	other := tt.matches("allocating")[1]
	winner := semi.Participants[1]
	tt.playBout(other, 1, other.Participants[0])

	r := tt.Do(http.MethodPost, "/api/v1/matches/"+semi.ID+"/resolve", tt.Organizer, map[string]any{"winner": winner}).Expect(http.StatusConflict)
	if r.Code() != "match-not-stalled" {
		t.Fatalf("code = %s", r.Code())
	}
	tt.Advance(10 * time.Minute)
	tt.Do(http.MethodPost, "/api/v1/matches/"+semi.ID+"/resolve", tt.Players[winner], map[string]any{"winner": winner}).Expect(http.StatusForbidden)

	tt.Do(http.MethodPost, "/api/v1/matches/"+semi.ID+"/resolve", tt.Organizer, map[string]any{"winner": winner}).Expect(http.StatusOK)
	tt.Settle()

	if m := tt.match(semi.ID); m.Status != "completed" || m.WinnerID != winner {
		t.Fatalf("semi = %+v, want won by %s", m, winner)
	}
	final := tt.matches("allocating")
	if len(final) != 1 || final[0].Key != "R2-M1" {
		t.Fatalf("want the final allocated, got %+v", final)
	}
}

func TestOrganizerResolvesAStalledMatchAsADoubleForfeit(t *testing.T) {
	tt := running(t, 2)
	final := tt.matches("allocating")[0]
	tt.Advance(10 * time.Minute)

	tt.Do(http.MethodPost, "/api/v1/matches/"+final.ID+"/resolve", tt.Organizer, map[string]any{"doubleForfeit": true}).Expect(http.StatusOK)

	if m := tt.match(final.ID); m.Result != "double-forfeit" {
		t.Fatalf("final = %+v", m)
	}
	if tt.get().Status != "completed" {
		t.Fatal("tournament should complete")
	}
	for _, p := range tt.placements() {
		if p.From != 1 || p.To != 2 {
			t.Fatalf("placements = %+v, want both sharing 1st–2nd", tt.placements())
		}
	}
}
