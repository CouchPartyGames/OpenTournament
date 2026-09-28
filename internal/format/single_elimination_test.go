package format_test

import (
	"reflect"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

var singleElim = format.Stage{Format: format.SingleElimination, BestOf: 1}

func TestSingleEliminationOpensFirstRoundWithFinalPending(t *testing.T) {
	gr := newGroup(t, singleElim, 4)

	p := gr.plan()

	got := participants(matchesIn(p, format.Playable))
	want := [][]format.ParticipantID{{"p1", "p4"}, {"p2", "p3"}}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("playable = %v, want %v", got, want)
	}
	if pending := matchesIn(p, format.Pending); len(pending) != 1 {
		t.Fatalf("want the final pending, got %v", pending)
	}
	if p.Complete {
		t.Fatal("group should not be complete")
	}
}

func TestSingleEliminationGivesByesToTopSeeds(t *testing.T) {
	gr := newGroup(t, singleElim, 6)

	p := gr.plan()

	var byes []format.ParticipantID
	for _, m := range matchesIn(p, format.Bye) {
		byes = append(byes, m.Outcome.Winner)
	}
	if want := []format.ParticipantID{"p1", "p2"}; !reflect.DeepEqual(byes, want) {
		t.Fatalf("byes = %v, want %v", byes, want)
	}
	// The Bye winners already wait in round 2 for their opponents.
	for _, m := range p.Matches {
		if m.Key == "R2-M1" && !reflect.DeepEqual(m.Participants, []format.ParticipantID{"p1"}) {
			t.Fatalf("R2-M1 participants = %v, want [p1]", m.Participants)
		}
	}
}

func TestSingleEliminationPlaysNMinusOneMatchesForEveryCount(t *testing.T) {
	for n := 2; n <= 70; n++ {
		gr := newGroup(t, singleElim, n)
		p := gr.playOut(lowerSeedWins)
		if played := len(matchesIn(p, format.Decided)); played != n-1 {
			t.Fatalf("n=%d: %d matches played, want %d", n, played, n-1)
		}
		if p.Standings[0].Participant != "p1" {
			t.Fatalf("n=%d: winner %s, want p1", n, p.Standings[0].Participant)
		}
	}
}

func TestSingleEliminationPlacementsShareRangesPerRound(t *testing.T) {
	gr := newGroup(t, singleElim, 8)

	p := gr.playOut(lowerSeedWins)

	got := placementsOf(p)
	want := map[format.ParticipantID][2]int{
		"p1": {1, 1}, "p2": {2, 2},
		"p3": {3, 4}, "p4": {3, 4},
		"p5": {5, 8}, "p6": {5, 8}, "p7": {5, 8}, "p8": {5, 8},
	}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("placements = %v, want %v", got, want)
	}
}

func TestSingleEliminationDoubleForfeitGivesNextOpponentABye(t *testing.T) {
	gr := newGroup(t, singleElim, 4)
	gr.plan()
	gr.doubleForfeit("R1-M1") // p1 vs p4
	gr.win("R1-M2", "p2")

	p := gr.plan()

	final := p.Matches[len(p.Matches)-1]
	if final.State != format.Bye || final.Outcome.Winner != "p2" {
		t.Fatalf("final = %+v, want a Bye for p2", final)
	}
	if !p.Complete {
		t.Fatal("group should be complete")
	}
	got := placementsOf(p)
	// p3 lost in round 1 like both forfeiting Participants, so all three share 2nd–4th.
	want := map[format.ParticipantID][2]int{"p2": {1, 1}, "p3": {2, 4}, "p1": {2, 4}, "p4": {2, 4}}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("placements = %v, want %v", got, want)
	}
}

func TestBestOfThreeEndsWhenOneParticipantWinsTwoBouts(t *testing.T) {
	ps := []format.ParticipantID{"a", "b"}
	bout := func(n int, winner format.ParticipantID) format.Bout {
		return format.Bout{Number: n, Results: []format.BoutResult{
			{Participant: "a", Won: winner == "a"}, {Participant: "b", Won: winner == "b"},
		}}
	}
	if _, done := format.HeadToHeadOutcome(3, ps, []format.Bout{bout(1, "a"), bout(2, "b")}); done {
		t.Fatal("1-1 should not be decided")
	}
	out, done := format.HeadToHeadOutcome(3, ps, []format.Bout{bout(1, "a"), bout(2, "b"), bout(3, "b")})
	if !done || out.Winner != "b" {
		t.Fatalf("outcome = %+v, %v; want b wins", out, done)
	}
}
