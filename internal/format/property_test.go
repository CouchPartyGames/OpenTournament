package format_test

import (
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
	"pgregory.net/rapid"
)

// Any head-to-head Group with any results completes, every Participant plays
// at most once per Round, and everyone gets a Placement.
func TestEveryHeadToHeadGroupCompletesWithRandomResults(t *testing.T) {
	rapid.Check(t, func(rt *rapid.T) {
		kind := rapid.SampledFrom([]format.Kind{
			format.SingleElimination, format.DoubleElimination, format.RoundRobin, format.Swiss,
		}).Draw(rt, "format")
		n := rapid.IntRange(2, 24).Draw(rt, "n")
		bestOf := rapid.SampledFrom([]int{1, 3}).Draw(rt, "bestOf")
		gr := newGroup(t, format.Stage{Format: kind, BestOf: bestOf}, n)
		pick := func(a, b format.ParticipantID) format.ParticipantID {
			if rapid.Bool().Draw(rt, "first wins") {
				return a
			}
			return b
		}

		p := gr.playOut(pick)

		perRound := map[string]map[format.ParticipantID]bool{}
		for _, m := range p.Matches {
			round := string(m.Bracket) + string(rune('0'+m.Round))
			if perRound[round] == nil {
				perRound[round] = map[format.ParticipantID]bool{}
			}
			for _, who := range m.Participants {
				if perRound[round][who] {
					rt.Fatalf("%s plays twice in round %s", who, round)
				}
				perRound[round][who] = true
			}
		}
		if len(p.Placements) != n {
			rt.Fatalf("%d placements for %d participants", len(p.Placements), n)
		}
	})
}
