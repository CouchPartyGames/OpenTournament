package format_test

import (
	"reflect"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

var swiss = format.Stage{Format: format.Swiss, BestOf: 1}

func pairKey(a, b format.ParticipantID) [2]format.ParticipantID {
	if a > b {
		a, b = b, a
	}
	return [2]format.ParticipantID{a, b}
}

func TestSwissPlaysCeilLog2RoundsByDefault(t *testing.T) {
	for n, rounds := range map[int]int{2: 1, 5: 3, 8: 3, 9: 4, 16: 4, 33: 6} {
		gr := newGroup(t, swiss, n)
		p := gr.playOut(lowerSeedWins)
		last := 0
		for _, m := range p.Matches {
			last = max(last, m.Round)
		}
		if last != rounds {
			t.Fatalf("n=%d: %d rounds, want %d", n, last, rounds)
		}
	}
}

func TestSwissRoundsCanBeOverridden(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.Swiss, BestOf: 1, SwissRounds: 5}, 8)
	p := gr.playOut(lowerSeedWins)
	if last := p.Matches[len(p.Matches)-1].Round; last != 5 {
		t.Fatalf("%d rounds, want 5", last)
	}
}

func TestSwissNeverRematchesAndEveryonePlaysOncePerRound(t *testing.T) {
	upsets := func(a, b format.ParticipantID) format.ParticipantID { return lowerSeedWins(b, a) }
	for n := 2; n <= 40; n++ {
		for _, pick := range []func(a, b format.ParticipantID) format.ParticipantID{lowerSeedWins, upsets} {
			gr := newGroup(t, swiss, n)
			p := gr.playOut(pick)
			met := map[[2]format.ParticipantID]bool{}
			perRound := map[int]map[format.ParticipantID]int{}
			byes := map[format.ParticipantID]int{}
			for _, m := range p.Matches {
				if perRound[m.Round] == nil {
					perRound[m.Round] = map[format.ParticipantID]int{}
				}
				for _, who := range m.Participants {
					perRound[m.Round][who]++
				}
				if m.State == format.Bye {
					byes[m.Outcome.Winner]++
					continue
				}
				k := pairKey(m.Participants[0], m.Participants[1])
				if met[k] {
					t.Fatalf("n=%d: rematch %v", n, k)
				}
				met[k] = true
			}
			for r, counts := range perRound {
				if len(counts) != n {
					t.Fatalf("n=%d: %d participants in round %d, want %d", n, len(counts), r, n)
				}
				for who, c := range counts {
					if c != 1 {
						t.Fatalf("n=%d: %s appears %d times in round %d", n, who, c, r)
					}
				}
			}
			for who, c := range byes {
				if c > 1 {
					t.Fatalf("n=%d: %s got %d byes", n, who, c)
				}
			}
		}
	}
}

func TestSwissByeGoesToLowestStandingWithoutAByeAndIsWorthAWin(t *testing.T) {
	gr := newGroup(t, swiss, 5)
	p := gr.plan()
	bye := matchesIn(p, format.Bye)
	if len(bye) != 1 || bye[0].Outcome.Winner != "p5" {
		t.Fatalf("round 1 byes = %+v, want one for the last seed p5", bye)
	}
	for _, m := range matchesIn(p, format.Playable) {
		gr.win(m.Key, lowerSeedWins(m.Participants[0], m.Participants[1]))
	}

	p = gr.plan()

	var bye2 format.ParticipantID
	for _, m := range matchesIn(p, format.Bye) {
		if m.Round == 2 {
			bye2 = m.Outcome.Winner
		}
	}
	// p5 already had a Bye, so it goes to the lowest-standing player without one.
	if bye2 == "" || bye2 == "p5" {
		t.Fatalf("round 2 bye = %q, want someone other than p5", bye2)
	}
	for _, s := range p.Standings {
		if s.Participant == "p5" && s.Points != 1 {
			t.Fatalf("p5 has %d points after a bye, want 1", s.Points)
		}
	}
}

func TestSwissPairsParticipantsWithEqualPoints(t *testing.T) {
	gr := newGroup(t, swiss, 8)
	p := gr.plan()
	winners := map[format.ParticipantID]bool{}
	for _, m := range matchesIn(p, format.Playable) {
		w := lowerSeedWins(m.Participants[0], m.Participants[1])
		winners[w] = true
		gr.win(m.Key, w)
	}

	p = gr.plan()

	for _, m := range matchesIn(p, format.Playable) {
		if winners[m.Participants[0]] != winners[m.Participants[1]] {
			t.Fatalf("round 2 pairs a winner with a loser: %v", m.Participants)
		}
	}
}

func TestSwissTiebreakersArePointsThenBuchholz(t *testing.T) {
	gr := newGroup(t, swiss, 4)
	p := gr.plan() // round 1: p1-p3, p2-p4
	gr.win(gr.keyOf(p, "p1", "p3"), "p1")
	gr.win(gr.keyOf(p, "p2", "p4"), "p4")
	p = gr.plan() // round 2: p1-p4 (1 point each), p2-p3 (0 points each)
	gr.win(gr.keyOf(p, "p1", "p4"), "p1")
	gr.win(gr.keyOf(p, "p2", "p3"), "p3")

	p = gr.plan()

	// p1: 2 points. p4 and p3: 1 point each; p4's opponents (p2: 0, p1: 2)
	// give Buchholz 2 and p3's (p1: 2, p2: 0) give 2 as well, so head-to-head
	// (they haven't met) and then the Lot decide: p3 has the lower Lot.
	if want := []format.ParticipantID{"p1", "p3", "p4", "p2"}; !reflect.DeepEqual(ranking(p), want) {
		t.Fatalf("ranking = %v, want %v", ranking(p), want)
	}
	for _, s := range p.Standings {
		if s.Participant == "p2" && s.Buchholz != 2 {
			t.Fatalf("p2 Buchholz = %d, want 2 (p4's point and p3's)", s.Buchholz)
		}
	}
}

func TestSwissDroppedParticipantIsNotPairedButStillCountsForBuchholz(t *testing.T) {
	gr := newGroup(t, swiss, 4)
	p := gr.plan() // p1-p3, p2-p4
	gr.win(gr.keyOf(p, "p1", "p3"), "p3")
	gr.win(gr.keyOf(p, "p2", "p4"), "p2")
	gr.g.Dropped = []format.ParticipantID{"p3"}

	p = gr.plan()

	for _, m := range p.Matches {
		if m.Round == 2 {
			for _, who := range m.Participants {
				if who == "p3" {
					t.Fatalf("dropped p3 paired in round 2: %+v", m)
				}
			}
		}
	}
	for _, s := range p.Standings {
		if s.Participant == "p1" && s.Buchholz != 1 {
			t.Fatalf("p1 Buchholz = %d, want 1 from the dropped p3's win", s.Buchholz)
		}
	}
}
