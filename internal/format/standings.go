package format

import "slices"

// record tallies the settled head-to-head Matches of a Group.
type record struct {
	stats     map[ParticipantID]*Standing
	beat      map[[2]ParticipantID]int // beat[{a, b}]: times a beat b
	opponents map[ParticipantID][]ParticipantID
	byes      map[ParticipantID]int
}

func newRecord(g Group) *record {
	r := &record{
		stats:     map[ParticipantID]*Standing{},
		beat:      map[[2]ParticipantID]int{},
		opponents: map[ParticipantID][]ParticipantID{},
		byes:      map[ParticipantID]int{},
	}
	dropped := droppedSet(g)
	for _, e := range g.Participants {
		r.stats[e.ID] = &Standing{Participant: e.ID, Dropped: dropped[e.ID]}
	}
	return r
}

// add counts a settled head-to-head Match. A Bye is worth a win.
func (r *record) add(pm PlannedMatch, bouts []Bout) {
	switch pm.State {
	case Bye:
		st := r.stats[pm.Outcome.Winner]
		st.Wins++
		st.Points++
		r.byes[pm.Outcome.Winner]++
	case Decided:
		a, b := pm.Participants[0], pm.Participants[1]
		r.opponents[a] = append(r.opponents[a], b)
		r.opponents[b] = append(r.opponents[b], a)
		won := BoutsWon(bouts)
		for _, p := range pm.Participants {
			st := r.stats[p]
			st.Played++
			other := a
			if p == a {
				other = b
			}
			st.BoutDifferential += won[p] - won[other]
			if p == pm.Outcome.Winner {
				st.Wins++
				st.Points++
				r.beat[[2]ParticipantID{p, other}]++
			} else {
				st.Losses++
			}
		}
	}
}

// ordered ranks Participants by primary, then separates each run of equal
// primary keys by head-to-head wins among that run, then by rest.
func ordered(ps []ParticipantID, beat map[[2]ParticipantID]int, primary, rest func(a, b ParticipantID) int) []ParticipantID {
	out := slices.Clone(ps)
	slices.SortStableFunc(out, func(a, b ParticipantID) int {
		if c := primary(a, b); c != 0 {
			return c
		}
		return rest(a, b)
	})
	for i := 0; i < len(out); {
		j := i + 1
		for j < len(out) && primary(out[i], out[j]) == 0 {
			j++
		}
		if j-i > 1 {
			run := out[i:j]
			mini := map[ParticipantID]int{}
			for _, x := range run {
				for _, y := range run {
					mini[x] += beat[[2]ParticipantID{x, y}]
				}
			}
			slices.SortStableFunc(run, func(a, b ParticipantID) int {
				if mini[a] != mini[b] {
					return mini[b] - mini[a]
				}
				return rest(a, b)
			})
		}
		i = j
	}
	return out
}

func lots(g Group) map[ParticipantID]int {
	out := make(map[ParticipantID]int, len(g.Participants))
	for _, e := range g.Participants {
		out[e.ID] = e.Lot
	}
	return out
}

func standingsIn(order []ParticipantID, stats map[ParticipantID]*Standing) []Standing {
	out := make([]Standing, 0, len(order))
	for i, p := range order {
		st := *stats[p]
		st.Position = i + 1
		out = append(out, st)
	}
	return out
}

// strictPlacements gives every Participant its own place, in Standing order.
func strictPlacements(standings []Standing) []Placement {
	out := make([]Placement, 0, len(standings))
	for _, s := range standings {
		out = append(out, Placement{Participant: s.Participant, From: s.Position, To: s.Position})
	}
	return out
}

func participantIDs(g Group) []ParticipantID {
	out := make([]ParticipantID, 0, len(g.Participants))
	for _, e := range g.Participants {
		out = append(out, e.ID)
	}
	return out
}
