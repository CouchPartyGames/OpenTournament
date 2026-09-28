package format_test

import (
	"math/rand/v2"
	"reflect"
	"slices"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

func ids(ss ...string) []format.ParticipantID {
	out := make([]format.ParticipantID, len(ss))
	for i, s := range ss {
		out[i] = format.ParticipantID(s)
	}
	return out
}

func TestRandomSeedingIsRepeatableForTheSameSource(t *testing.T) {
	ps := ids("a", "b", "c", "d", "e", "f", "g", "h")
	one := format.RandomSeeding(ps, rand.New(rand.NewPCG(1, 2)))
	two := format.RandomSeeding(ps, rand.New(rand.NewPCG(1, 2)))
	if !reflect.DeepEqual(one, two) {
		t.Fatalf("%v != %v", one, two)
	}
	sorted := slices.Clone(one)
	slices.Sort(sorted)
	if !reflect.DeepEqual(sorted, ps) {
		t.Fatalf("seeding %v is not a permutation of %v", one, ps)
	}
}

func TestSnakeDistributionBalancesGroups(t *testing.T) {
	got := format.Snake(ids("1", "2", "3", "4", "5", "6", "7"), 3)
	want := [][]format.ParticipantID{ids("1", "6", "7"), ids("2", "5"), ids("3", "4")}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("snake = %v, want %v", got, want)
	}
	for n := 1; n <= 50; n++ {
		for g := 1; g <= 8; g++ {
			ps := make([]format.ParticipantID, n)
			for i := range ps {
				ps[i] = pid(i + 1)
			}
			sizes := format.GroupSizes(n, g)
			groups := format.Snake(ps, g)
			for i, grp := range groups {
				if len(grp) != sizes[i] {
					t.Fatalf("n=%d g=%d: group %d has %d, GroupSizes says %d", n, g, i, len(grp), sizes[i])
				}
			}
			if slices.Max(sizes)-slices.Min(sizes) > 1 {
				t.Fatalf("n=%d g=%d: sizes %v differ by more than one", n, g, sizes)
			}
		}
	}
}

func TestAdvancementSeedsGroupWinnersAboveRunnersUpAndSkipsDropped(t *testing.T) {
	a := []format.Standing{{Participant: "a1", Position: 1}, {Participant: "a2", Position: 2, Dropped: true}, {Participant: "a3", Position: 3}}
	b := []format.Standing{{Participant: "b1", Position: 1}, {Participant: "b2", Position: 2}, {Participant: "b3", Position: 3}}

	got := format.AdvancementSeeding(singleElim, 1, [][]format.Standing{a, b}, 2)

	// a2 withdrew, so a3 advances in its place.
	if want := ids("a1", "b1", "a3", "b2"); !reflect.DeepEqual(got, want) {
		t.Fatalf("seeding = %v, want %v", got, want)
	}
	// In a single-elimination bracket of four, group mates don't meet in round 1.
	gr := &group{t: t, g: format.Group{Stage: singleElim}}
	for i, p := range got {
		gr.g.Participants = append(gr.g.Participants, format.Participant{ID: p, Lot: i})
	}
	for _, m := range gr.plan().Matches[:2] {
		if m.Participants[0][0] == m.Participants[1][0] {
			t.Fatalf("group mates meet in round 1: %v", m.Participants)
		}
	}
}

func TestFinalPlacementsRankLaterStagesAboveEarlierOnes(t *testing.T) {
	swissStage := []format.GroupResult{
		{
			Placements: []format.Placement{{"a", 1, 1}, {"b", 2, 2}, {"c", 3, 3}, {"d", 4, 4}},
			Advanced:   ids("a", "b"),
		},
		{
			Placements: []format.Placement{{"e", 1, 1}, {"f", 2, 2}, {"g", 3, 3}, {"h", 4, 4}},
			Advanced:   ids("e", "f"),
		},
	}
	final := []format.GroupResult{{
		Placements: []format.Placement{{"a", 1, 1}, {"e", 2, 2}, {"b", 3, 4}, {"f", 3, 4}},
	}}

	got := format.FinalPlacements([][]format.GroupResult{swissStage, final})

	want := []format.Placement{
		{"a", 1, 1}, {"e", 2, 2}, {"b", 3, 4}, {"f", 3, 4},
		// The 3rd of each Swiss Group share 5th–6th, the 4th share 7th–8th.
		{"c", 5, 6}, {"g", 5, 6}, {"d", 7, 8}, {"h", 7, 8},
	}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("placements = %v, want %v", got, want)
	}
}

func standingsOf(names ...string) []format.Standing {
	out := make([]format.Standing, len(names))
	for i, n := range names {
		out[i] = format.Standing{Participant: format.ParticipantID(n), Position: i + 1}
	}
	return out
}

func TestAdvancementKeepsGroupMatesApartInTheFirstRoundWherePossible(t *testing.T) {
	for _, tc := range []struct {
		groups     [][]format.Standing
		advance    int
		nextGroups int
	}{
		{[][]format.Standing{standingsOf("a1", "a2", "a3"), standingsOf("b1", "b2", "b3"), standingsOf("c1", "c2", "c3")}, 2, 1},
		{[][]format.Standing{standingsOf("a1", "a2", "a3"), standingsOf("b1", "b2", "b3")}, 2, 1},
		{[][]format.Standing{standingsOf("a1", "a2", "a3", "a4"), standingsOf("b1", "b2", "b3", "b4"), standingsOf("c1", "c2", "c3", "c4")}, 4, 2},
	} {
		next := format.Stage{Format: format.SingleElimination, BestOf: 1}

		seeds := format.AdvancementSeeding(next, tc.nextGroups, tc.groups, tc.advance)

		for _, members := range format.Snake(seeds, tc.nextGroups) {
			g := format.Group{Stage: next}
			for i, p := range members {
				g.Participants = append(g.Participants, format.Participant{ID: p, Lot: i})
			}
			p, err := format.PlanGroup(g)
			if err != nil {
				t.Fatal(err)
			}
			for _, m := range p.Matches {
				if m.Round == 1 && len(m.Participants) == 2 && m.Participants[0][0] == m.Participants[1][0] {
					t.Errorf("AdvancementSeeding(%d groups → %d) pairs group mates %v in round 1 (seeds %v)",
						len(tc.groups), tc.nextGroups, m.Participants, seeds)
				}
			}
		}
		// Every Group winner is still seeded above every runner-up.
		for i, p := range seeds[:len(tc.groups)] {
			if p[1] != '1' {
				t.Errorf("seed %d is %s, want a group winner", i+1, p)
			}
		}
	}
}
