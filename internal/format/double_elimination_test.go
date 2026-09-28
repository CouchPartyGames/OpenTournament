package format_test

import (
	"reflect"
	"slices"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

var doubleElim = format.Stage{Format: format.DoubleElimination, BestOf: 1}

func TestDoubleEliminationUpperBracketLoserDropsToLowerBracket(t *testing.T) {
	gr := newGroup(t, doubleElim, 4)
	gr.plan()
	gr.win("U1-M1", "p1") // p1 beats p4
	gr.win("U1-M2", "p3") // p3 upsets p2

	p := gr.plan()

	lower := matchesIn(p, format.Playable)
	var got [][]format.ParticipantID
	for _, m := range lower {
		if m.Bracket == format.Lower {
			got = append(got, m.Participants)
		}
	}
	if want := [][]format.ParticipantID{{"p4", "p2"}}; !reflect.DeepEqual(got, want) {
		t.Fatalf("playable lower-bracket matches = %v, want %v", got, want)
	}
	for _, s := range p.Standings {
		if s.Eliminated {
			t.Fatalf("%s eliminated after one loss", s.Participant)
		}
	}
}

func TestDoubleEliminationWithoutBracketResetWhenUpperFinalistWins(t *testing.T) {
	gr := newGroup(t, doubleElim, 4)

	p := gr.playOut(lowerSeedWins)

	if slices.ContainsFunc(p.Matches, func(m format.PlannedMatch) bool { return m.Key == "GF-M2" }) {
		t.Fatal("no bracket reset expected")
	}
	want := map[format.ParticipantID][2]int{"p1": {1, 1}, "p2": {2, 2}, "p3": {3, 3}, "p4": {4, 4}}
	if got := placementsOf(p); !reflect.DeepEqual(got, want) {
		t.Fatalf("placements = %v, want %v", got, want)
	}
}

func TestDoubleEliminationBracketResetWhenLowerFinalistWinsGrandFinal(t *testing.T) {
	gr := newGroup(t, doubleElim, 4)
	gr.plan()
	gr.win("U1-M1", "p1")
	gr.win("U1-M2", "p2")
	gr.plan()
	gr.win("U2-M1", "p1") // p2 drops to the lower final
	gr.win("L1-M1", "p3")
	gr.plan()
	gr.win("L2-M1", "p2")
	gr.plan()
	gr.win("GF-M1", "p2") // the undefeated p1 loses for the first time

	p := gr.plan()

	if p.Complete {
		t.Fatal("group should wait for the bracket reset")
	}
	reset := p.Matches[len(p.Matches)-1]
	if reset.Key != "GF-M2" || reset.State != format.Playable {
		t.Fatalf("last match = %+v, want a playable GF-M2", reset)
	}
	for _, s := range p.Standings {
		if s.Participant == "p1" && s.Eliminated {
			t.Fatal("p1 must not be eliminated after a single loss")
		}
	}

	gr.plan()
	gr.win("GF-M2", "p2")
	p = gr.plan()

	if !p.Complete || p.Standings[0].Participant != "p2" {
		t.Fatalf("want p2 to win after the reset, complete=%v standings=%+v", p.Complete, p.Standings)
	}
	if got := placementsOf(p)["p1"]; got != [2]int{2, 2} {
		t.Fatalf("p1 placement = %v, want 2nd", got)
	}
}

func TestDoubleEliminationEveryParticipantButTheWinnerLosesTwiceOrOnceInGrandFinal(t *testing.T) {
	upsets := func(a, b format.ParticipantID) format.ParticipantID { return lowerSeedWins(b, a) }
	for n := 2; n <= 40; n++ {
		for _, pick := range []func(a, b format.ParticipantID) format.ParticipantID{lowerSeedWins, upsets} {
			gr := newGroup(t, doubleElim, n)
			p := gr.playOut(pick)
			played := len(matchesIn(p, format.Decided))
			// Everyone but the champion loses twice, except a champion who never
			// lost (2n-2 Matches) or who forced a Bracket Reset (2n-1).
			if played != 2*n-2 && played != 2*n-1 {
				t.Fatalf("n=%d: %d matches played, want 2n-2 or 2n-1", n, played)
			}
			if len(p.Placements) != n || p.Placements[0].From != 1 || p.Placements[0].To != 1 {
				t.Fatalf("n=%d: placements %v", n, p.Placements)
			}
		}
	}
}
