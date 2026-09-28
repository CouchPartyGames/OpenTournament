package format

import (
	"cmp"
	"fmt"
	"math/bits"
	"slices"
)

// DefaultSwissRounds is ⌈log₂ n⌉, and at least 1.
func DefaultSwissRounds(n int) int {
	if n <= 2 {
		return 1
	}
	return bits.Len(uint(n - 1))
}

// SwissRoundsFor is the number of Rounds a Swiss Stage plays with n Participants.
func (s Stage) SwissRoundsFor(n int) int {
	if s.SwissRounds > 0 {
		return s.SwissRounds
	}
	return DefaultSwissRounds(n)
}

func planSwiss(g Group) Plan {
	rounds := g.Stage.SwissRoundsFor(len(g.Participants))
	existing := slices.Clone(g.Matches)
	slices.SortStableFunc(existing, func(a, b Match) int {
		return cmp.Or(cmp.Compare(a.Round, b.Round), cmp.Compare(a.Key, b.Key))
	})

	var plan Plan
	rec := newRecord(g)
	created, settled := 0, true
	for _, m := range existing {
		pm := PlannedMatch{Key: m.Key, Round: m.Round, Participants: m.Participants, State: Playable}
		if len(m.Participants) == 1 {
			pm.State = Bye
			pm.Outcome = &Outcome{Winner: m.Participants[0]}
		} else if out, ok := HeadToHeadOutcome(g.Stage.BestOf, m.Participants, m.Bouts); ok {
			pm.State = Decided
			pm.Outcome = &out
		}
		if pm.State.Settled() {
			rec.add(pm, m.Bouts)
		} else {
			settled = false
		}
		created = max(created, m.Round)
		plan.Matches = append(plan.Matches, pm)
	}

	order := swissOrder(g, rec)
	switch {
	case !settled:
	case created >= rounds:
		plan.Complete = true
	default:
		next := swissPairings(g, rec, order, created+1)
		plan.Matches = append(plan.Matches, next...)
		// A Round with nobody left to pair ends the Stage early.
		plan.Complete = len(next) == 0
	}
	plan.Standings = standingsIn(order, rec.stats)
	if plan.Complete {
		plan.Placements = strictPlacements(plan.Standings)
	}
	return plan
}

// swissOrder ranks by points and Buchholz, then head-to-head, then Lot.
func swissOrder(g Group, rec *record) []ParticipantID {
	for p, st := range rec.stats {
		st.Buchholz = 0
		for _, o := range rec.opponents[p] {
			st.Buchholz += rec.stats[o].Points
		}
	}
	lot := lots(g)
	return ordered(participantIDs(g), rec.beat,
		func(a, b ParticipantID) int {
			sa, sb := rec.stats[a], rec.stats[b]
			return cmp.Or(sb.Points-sa.Points, sb.Buchholz-sa.Buchholz)
		},
		func(a, b ParticipantID) int { return lot[a] - lot[b] })
}

// swissPairings pairs the next Round among Participants who haven't dropped.
// With an odd count, the lowest-standing Participant without a Bye gets one.
func swissPairings(g Group, rec *record, order []ParticipantID, round int) []PlannedMatch {
	dropped := droppedSet(g)
	var active []ParticipantID
	if round == 1 {
		order = participantIDs(g)
	}
	for _, p := range order {
		if !dropped[p] {
			active = append(active, p)
		}
	}
	var out []PlannedMatch
	if len(active)%2 == 1 {
		pick := len(active) - 1
		for i := len(active) - 1; i >= 0; i-- {
			if rec.byes[active[i]] == 0 {
				pick = i
				break
			}
		}
		out = append(out, PlannedMatch{
			Key: fmt.Sprintf("R%d-BYE", round), Round: round,
			Participants: []ParticipantID{active[pick]},
			State:        Bye, Outcome: &Outcome{Winner: active[pick]},
		})
		active = slices.Delete(active, pick, pick+1)
	}

	var pairs [][2]ParticipantID
	if round == 1 {
		// Top half against bottom half.
		half := len(active) / 2
		for i := range half {
			pairs = append(pairs, [2]ParticipantID{active[i], active[i+half]})
		}
	} else {
		played := map[[2]ParticipantID]bool{}
		for p, os := range rec.opponents {
			for _, o := range os {
				played[[2]ParticipantID{p, o}] = true
			}
		}
		budget := 200_000
		var ok bool
		if pairs, ok = pairAvoidingRematches(active, played, &budget); !ok {
			pairs = nil
			for i := 0; i+1 < len(active); i += 2 {
				pairs = append(pairs, [2]ParticipantID{active[i], active[i+1]})
			}
		}
	}
	for i, pr := range pairs {
		out = append(out, PlannedMatch{
			Key: fmt.Sprintf("R%d-M%d", round, i+1), Round: round,
			Participants: []ParticipantID{pr[0], pr[1]}, State: Playable,
		})
	}
	if len(out) > 0 && out[0].State == Bye {
		// Keep the Bye last, after the real Matches.
		out = append(out[1:], out[0])
	}
	return out
}

// pairAvoidingRematches pairs each Participant, top of the Standing first,
// with the nearest one below it that it hasn't played, backtracking when the
// rest can't be paired.
func pairAvoidingRematches(ps []ParticipantID, played map[[2]ParticipantID]bool, budget *int) ([][2]ParticipantID, bool) {
	if len(ps) == 0 {
		return nil, true
	}
	a := ps[0]
	for i := 1; i < len(ps); i++ {
		b := ps[i]
		if played[[2]ParticipantID{a, b}] {
			continue
		}
		if *budget--; *budget < 0 {
			return nil, false
		}
		rest := make([]ParticipantID, 0, len(ps)-2)
		rest = append(rest, ps[1:i]...)
		rest = append(rest, ps[i+1:]...)
		if pairs, ok := pairAvoidingRematches(rest, played, budget); ok {
			return append([][2]ParticipantID{{a, b}}, pairs...), true
		}
	}
	return nil, false
}
