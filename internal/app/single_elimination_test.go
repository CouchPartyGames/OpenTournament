package app_test

import (
	"testing"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

func TestSingleEliminationTournamentRunsFromRegistrationToFinalPlacements(t *testing.T) {
	h := apptest.MustStart(t)
	tt := mustCreate(t, h, settings(h))
	tt.openRegistration()
	tt.mustRegister("anna", "bert", "cleo", "dave")

	tt.start()

	if got := tt.get().Status; got != "running" {
		t.Fatalf("status = %s, want running", got)
	}
	semis := tt.matches("allocating")
	if len(semis) != 2 || len(tt.matches("pending")) != 1 {
		t.Fatalf("want two allocated semifinals and a pending final, got %+v", tt.matches(""))
	}
	for _, m := range semis {
		if !m.ServerAllocated {
			t.Fatalf("semifinal %s has no game server", m.Key)
		}
	}

	tt.playAll(tt.byName)

	if got := tt.get().Status; got != "completed" {
		t.Fatalf("status = %s, want completed", got)
	}
	if got := tt.placementOf("anna"); got != [2]int{1, 1} {
		t.Fatalf("anna placed %v, want 1st", got)
	}
	losers := 0
	for _, p := range tt.placements() {
		if p.From == 3 && p.To == 4 {
			losers++
		}
	}
	if losers != 2 {
		t.Fatalf("want both semifinal losers to share 3rd–4th, placements %+v", tt.placements())
	}
	if running := h.FakeServers.Running(); running != 0 {
		t.Fatalf("%d game servers still allocated after the tournament", running)
	}
}
