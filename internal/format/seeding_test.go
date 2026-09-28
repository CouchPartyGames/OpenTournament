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
	a := []format.Standing{{Participant: "a1", Rank: 1}, {Participant: "a2", Rank: 2, Dropped: true}, {Participant: "a3", Rank: 3}}
	b := []format.Standing{{Participant: "b1", Rank: 1}, {Participant: "b2", Rank: 2}, {Participant: "b3", Rank: 3}}

	got := format.AdvancementSeeding([][]format.Standing{a, b}, 2)

	// a2 withdrew, so a3 advances in its place.
	if want := ids("a1", "b1", "a3", "b2"); !reflect.DeepEqual(got, want) {
		t.Fatalf("seeding = %v, want %v", got, want)
	}
	// In a single-elimination bracket of four, group mates don't meet in round 1.
	gr := &group{t: t, g: format.Group{Stage: singleElim}}
	for i, p := range got {
		gr.g.Entrants = append(gr.g.Entrants, format.Entrant{ID: p, Lot: i})
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
