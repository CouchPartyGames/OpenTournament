package format_test

import (
	"reflect"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

var roundRobin = format.Stage{Format: format.RoundRobin, BestOf: 1}

func TestRoundRobinEveryoneMeetsEveryoneExactlyOnce(t *testing.T) {
	for n := 2; n <= 21; n++ {
		gr := newGroup(t, roundRobin, n)
		p := gr.playOut(lowerSeedWins)

		met := map[[2]format.ParticipantID]int{}
		perRound := map[int]map[format.ParticipantID]int{}
		for _, m := range p.Matches {
			a, b := m.Participants[0], m.Participants[1]
			if a > b {
				a, b = b, a
			}
			met[[2]format.ParticipantID{a, b}]++
			if perRound[m.Round] == nil {
				perRound[m.Round] = map[format.ParticipantID]int{}
			}
			perRound[m.Round][a]++
			perRound[m.Round][b]++
		}
		if len(met) != n*(n-1)/2 {
			t.Fatalf("n=%d: %d distinct pairings, want %d", n, len(met), n*(n-1)/2)
		}
		for pair, c := range met {
			if c != 1 {
				t.Fatalf("n=%d: %v met %d times", n, pair, c)
			}
		}
		for r, counts := range perRound {
			for who, c := range counts {
				if c > 1 {
					t.Fatalf("n=%d: %s plays %d times in round %d", n, who, c, r)
				}
			}
		}
	}
}

func TestRoundRobinNextRoundWaitsForTheWholeRound(t *testing.T) {
	gr := newGroup(t, roundRobin, 4)
	p := gr.plan()
	first := matchesIn(p, format.Playable)
	if len(first) != 2 || first[0].Round != 1 || first[1].Round != 1 {
		t.Fatalf("playable = %+v, want the two round 1 matches", first)
	}

	gr.win(first[0].Key, first[0].Participants[0])
	p = gr.plan()
	if got := matchesIn(p, format.Playable); len(got) != 1 || got[0].Round != 1 {
		t.Fatalf("playable = %+v, want only the unfinished round 1 match", got)
	}

	gr.win(first[1].Key, first[1].Participants[0])
	p = gr.plan()
	for _, m := range matchesIn(p, format.Playable) {
		if m.Round != 2 {
			t.Fatalf("round %d match playable, want round 2", m.Round)
		}
	}
	if len(matchesIn(p, format.Playable)) != 2 {
		t.Fatal("want both round 2 matches playable")
	}
}

func TestRoundRobinTiebreakersAreHeadToHeadThenBoutDifferential(t *testing.T) {
	stage := format.Stage{Format: format.RoundRobin, BestOf: 3}
	gr := newGroup(t, stage, 4)
	p := gr.plan()
	// p1 beats everyone. p2, p3 and p4 beat each other in a cycle, so head-to-head
	// can't separate them and Bout differential decides.
	gr.score(gr.keyOf(p, "p1", "p2"), "p1", 0)
	gr.score(gr.keyOf(p, "p1", "p3"), "p1", 0)
	gr.score(gr.keyOf(p, "p1", "p4"), "p1", 0)
	gr.score(gr.keyOf(p, "p2", "p3"), "p2", 0) // p2 +2
	gr.score(gr.keyOf(p, "p3", "p4"), "p3", 1) // p3 +1
	gr.score(gr.keyOf(p, "p4", "p2"), "p4", 1) // p4 +1, p2 -1
	// Bout differentials: p2 -2+2-1 = -1, p3 -2-2+1 = -3, p4 -2-1+1 = -2.

	p = gr.plan()

	if want := []format.ParticipantID{"p1", "p2", "p4", "p3"}; !reflect.DeepEqual(ranking(p), want) {
		t.Fatalf("ranking = %v, want %v", ranking(p), want)
	}
	if !p.Complete {
		t.Fatal("group should be complete")
	}
}

func TestRoundRobinHeadToHeadBeatsBoutDifferential(t *testing.T) {
	stage := format.Stage{Format: format.RoundRobin, BestOf: 3}
	gr := newGroup(t, stage, 4)
	p := gr.plan()
	gr.score(gr.keyOf(p, "p1", "p2"), "p2", 1) // p2 beats p1 narrowly
	gr.score(gr.keyOf(p, "p1", "p3"), "p1", 0)
	gr.score(gr.keyOf(p, "p1", "p4"), "p1", 0)
	gr.score(gr.keyOf(p, "p2", "p3"), "p3", 0)
	gr.score(gr.keyOf(p, "p2", "p4"), "p2", 1)
	gr.score(gr.keyOf(p, "p3", "p4"), "p4", 0)
	// p1 and p2 both have two wins. p1's Bout differential is +3 and p2's is 0,
	// but p2 beat p1.
	p = gr.plan()

	if got := ranking(p)[:2]; !reflect.DeepEqual(got, []format.ParticipantID{"p2", "p1"}) {
		t.Fatalf("top two = %v, want p2 above p1 on head-to-head", got)
	}
}
