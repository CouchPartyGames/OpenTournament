package format

import (
	"cmp"
	"math/rand/v2"
	"slices"
)

// RandomSeeding orders Participants randomly, for the first Stage.
func RandomSeeding(ps []ParticipantID, r *rand.Rand) []ParticipantID {
	out := slices.Clone(ps)
	r.Shuffle(len(out), func(i, j int) { out[i], out[j] = out[j], out[i] })
	return out
}

// DrawLots turns seeded Participants into Entrants with a random Lot each.
func DrawLots(seeds []ParticipantID, r *rand.Rand) []Entrant {
	perm := r.Perm(len(seeds))
	out := make([]Entrant, len(seeds))
	for i, p := range seeds {
		out[i] = Entrant{ID: p, Lot: perm[i]}
	}
	return out
}

// GroupSizes is how many Participants each Group gets when n are distributed
// by Snake. Sizes differ by at most one.
func GroupSizes(n, groups int) []int {
	sizes := make([]int, groups)
	for i := range n {
		sizes[snakeGroup(i, groups)]++
	}
	return sizes
}

func snakeGroup(i, groups int) int {
	row, col := i/groups, i%groups
	if row%2 == 1 {
		return groups - 1 - col
	}
	return col
}

// Snake distributes seeds into Groups: 1..G forwards, then G..1 backwards,
// and so on, so that Group strength is balanced.
func Snake(seeds []ParticipantID, groups int) [][]ParticipantID {
	out := make([][]ParticipantID, groups)
	for i, p := range seeds {
		g := snakeGroup(i, groups)
		out[g] = append(out[g], p)
	}
	return out
}

// Advancing returns the top n Participants of a Group's Standings, skipping
// those who dropped.
func Advancing(standings []Standing, n int) []ParticipantID {
	var out []ParticipantID
	for _, s := range standings {
		if len(out) == n {
			break
		}
		if !s.Dropped {
			out = append(out, s.Participant)
		}
	}
	return out
}

// AdvancementSeeding seeds the next Stage from the previous Stage's Groups:
// every Group winner, then every runner-up, and so on.
func AdvancementSeeding(groups [][]Standing, n int) []ParticipantID {
	advancing := make([][]ParticipantID, len(groups))
	for i, s := range groups {
		advancing[i] = Advancing(s, n)
	}
	var out []ParticipantID
	for place := range n {
		for _, a := range advancing {
			if place < len(a) {
				out = append(out, a[place])
			}
		}
	}
	return out
}

// GroupResult is how one completed Group ended.
type GroupResult struct {
	Placements []Placement
	Advanced   []ParticipantID
}

// FinalPlacements ranks everyone in the Tournament. Stages are in order;
// Participants who got further rank above everyone knocked out earlier.
// Within a Stage, Participants who finished in the same position of their
// Groups share a range.
func FinalPlacements(stages [][]GroupResult) []Placement {
	var out []Placement
	for s := len(stages) - 1; s >= 0; s-- {
		type entry struct {
			p    ParticipantID
			tier int
		}
		var stage []entry
		for _, g := range stages[s] {
			left := slices.DeleteFunc(slices.Clone(g.Placements), func(pl Placement) bool {
				return slices.Contains(g.Advanced, pl.Participant)
			})
			slices.SortStableFunc(left, func(a, b Placement) int { return cmp.Compare(a.From, b.From) })
			// Re-rank within the Group once the advancing Participants are gone.
			for i, pl := range left {
				tier := i
				if i > 0 && pl.From == left[i-1].From {
					tier = stage[len(stage)-1].tier
				}
				stage = append(stage, entry{pl.Participant, tier})
			}
		}
		slices.SortStableFunc(stage, func(a, b entry) int { return cmp.Compare(a.tier, b.tier) })
		offset := len(out)
		for i := 0; i < len(stage); {
			j := i
			for j < len(stage) && stage[j].tier == stage[i].tier {
				j++
			}
			for _, e := range stage[i:j] {
				out = append(out, Placement{Participant: e.p, From: offset + i + 1, To: offset + j})
			}
			i = j
		}
	}
	return out
}
