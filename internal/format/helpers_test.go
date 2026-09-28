package format_test

import (
	"fmt"
	"slices"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

// seeded builds n Participants named p1..pn in Seeding order, with Lots equal to their seed.
func seeded(n int) []format.Participant {
	es := make([]format.Participant, n)
	for i := range es {
		es[i] = format.Participant{ID: pid(i + 1), Lot: i}
	}
	return es
}

func pid(seed int) format.ParticipantID { return format.ParticipantID(fmt.Sprintf("p%d", seed)) }

// group is a stand-in for the persisted state of one Group: it stores the
// Matches the engine asked for, and the Bouts played in them.
type group struct {
	t *testing.T
	g format.Group
}

func newGroup(t *testing.T, stage format.Stage, n int) *group {
	t.Helper()
	return &group{t: t, g: format.Group{Stage: stage, Participants: seeded(n)}}
}

// plan asks the engine for the current plan and persists any newly created
// Matches, with their current Participants, the way the app would.
func (gr *group) plan() format.Plan {
	gr.t.Helper()
	p, err := format.PlanGroup(gr.g)
	if err != nil {
		gr.t.Fatalf("PlanGroup: %v", err)
	}
	for _, m := range p.Matches {
		i := slices.IndexFunc(gr.g.Matches, func(s format.Match) bool { return s.Key == m.Key })
		if i < 0 {
			gr.g.Matches = append(gr.g.Matches, format.Match{Key: m.Key, Round: m.Round})
			i = len(gr.g.Matches) - 1
		}
		gr.g.Matches[i].Participants = slices.Clone(m.Participants)
	}
	return p
}

func (gr *group) match(key string) *format.Match {
	gr.t.Helper()
	i := slices.IndexFunc(gr.g.Matches, func(s format.Match) bool { return s.Key == key })
	if i < 0 {
		gr.t.Fatalf("no match %q", key)
	}
	return &gr.g.Matches[i]
}

// win records Bouts in a head-to-head Match until winner has won it.
func (gr *group) win(key string, winner format.ParticipantID) {
	gr.t.Helper()
	m := gr.match(key)
	need := gr.g.Stage.BestOf/2 + 1
	for w := 0; w < need; w++ {
		var results []format.BoutResult
		for _, p := range m.Participants {
			results = append(results, format.BoutResult{Participant: p, Won: p == winner})
		}
		m.Bouts = append(m.Bouts, format.Bout{Number: len(m.Bouts) + 1, Results: results})
	}
}

// doubleForfeit records a Bout that both Participants forfeited.
func (gr *group) doubleForfeit(key string) {
	gr.t.Helper()
	m := gr.match(key)
	var results []format.BoutResult
	for _, p := range m.Participants {
		results = append(results, format.BoutResult{Participant: p, Forfeited: true})
	}
	m.Bouts = append(m.Bouts, format.Bout{Number: len(m.Bouts) + 1, Results: results})
}

// playOut plays every playable Match until the Group completes. pick chooses
// the winner of each head-to-head Match.
func (gr *group) playOut(pick func(a, b format.ParticipantID) format.ParticipantID) format.Plan {
	gr.t.Helper()
	for range 10_000 {
		p := gr.plan()
		if p.Complete {
			return p
		}
		playable := 0
		for _, m := range p.Matches {
			if m.State == format.Playable {
				playable++
				gr.win(m.Key, pick(m.Participants[0], m.Participants[1]))
			}
		}
		if playable == 0 {
			gr.t.Fatalf("group is stuck: no playable match and not complete")
		}
	}
	gr.t.Fatalf("group did not complete")
	return format.Plan{}
}

// lowerSeedWins makes the better-seeded Participant (p1 beats p2) win.
func lowerSeedWins(a, b format.ParticipantID) format.ParticipantID {
	var sa, sb int
	fmt.Sscanf(string(a), "p%d", &sa)
	fmt.Sscanf(string(b), "p%d", &sb)
	if sa < sb {
		return a
	}
	return b
}

func matchesIn(p format.Plan, state format.MatchState) []format.PlannedMatch {
	var out []format.PlannedMatch
	for _, m := range p.Matches {
		if m.State == state {
			out = append(out, m)
		}
	}
	return out
}

func participants(ms []format.PlannedMatch) [][]format.ParticipantID {
	var out [][]format.ParticipantID
	for _, m := range ms {
		out = append(out, m.Participants)
	}
	return out
}

func placementsOf(p format.Plan) map[format.ParticipantID][2]int {
	out := map[format.ParticipantID][2]int{}
	for _, pl := range p.Placements {
		out[pl.Participant] = [2]int{pl.From, pl.To}
	}
	return out
}

// score records a head-to-head Match that winner took after losing `lost` Bouts.
func (gr *group) score(key string, winner format.ParticipantID, lost int) {
	gr.t.Helper()
	m := gr.match(key)
	add := func(w format.ParticipantID) {
		var results []format.BoutResult
		for _, p := range m.Participants {
			results = append(results, format.BoutResult{Participant: p, Won: p == w})
		}
		m.Bouts = append(m.Bouts, format.Bout{Number: len(m.Bouts) + 1, Results: results})
	}
	loser := m.Participants[0]
	if loser == winner {
		loser = m.Participants[1]
	}
	for range lost {
		add(loser)
	}
	for range gr.g.Stage.BestOf/2 + 1 {
		add(winner)
	}
}

// keyOf finds the key of the Match between a and b.
func (gr *group) keyOf(p format.Plan, a, b format.ParticipantID) string {
	gr.t.Helper()
	for _, m := range p.Matches {
		if slices.Contains(m.Participants, a) && slices.Contains(m.Participants, b) {
			return m.Key
		}
	}
	gr.t.Fatalf("no match between %s and %s", a, b)
	return ""
}

func ranking(p format.Plan) []format.ParticipantID {
	var out []format.ParticipantID
	for _, s := range p.Standings {
		out = append(out, s.Participant)
	}
	return out
}
