package format

import "cmp"

// FreeForAllKey is the key of the one Match of a free-for-all Group.
const FreeForAllKey = "M1"

// FreeForAllDecided reports whether a free-for-all Match is over: every Bout
// has been played, or at most one Participant is left who hasn't dropped.
func FreeForAllDecided(bouts int, participants []ParticipantID, played []Bout, dropped map[ParticipantID]bool) bool {
	left := 0
	for _, p := range participants {
		if !dropped[p] {
			left++
		}
	}
	return left <= 1 || len(completeBouts(participants, played, dropped)) >= bouts
}

// completeBouts are the Bouts with a result for every Participant. A dropped
// Participant with no result forfeited that Bout.
func completeBouts(participants []ParticipantID, played []Bout, dropped map[ParticipantID]bool) []Bout {
	var out []Bout
	for _, b := range played {
		has := map[ParticipantID]bool{}
		for _, r := range b.Results {
			has[r.Participant] = true
		}
		complete := true
		for _, p := range participants {
			if !has[p] && !dropped[p] {
				complete = false
			}
		}
		if complete {
			out = append(out, b)
		}
	}
	return out
}

func planFreeForAll(g Group) Plan {
	ids := entrantIDs(g)
	dropped := droppedSet(g)
	bouts := persisted(g)[FreeForAllKey].Bouts
	pm := PlannedMatch{Key: FreeForAllKey, Round: 1, Participants: ids, State: Playable}
	if FreeForAllDecided(g.Stage.Bouts, ids, bouts, dropped) {
		pm.State = Decided
		pm.Outcome = &Outcome{}
	}

	stats := map[ParticipantID]*Standing{}
	for _, p := range ids {
		stats[p] = &Standing{Participant: p, Dropped: dropped[p]}
	}
	// A forfeited Bout scores last placement and no points.
	last := len(ids)
	for _, b := range completeBouts(ids, bouts, dropped) {
		byP := map[ParticipantID]BoutResult{}
		for _, r := range b.Results {
			byP[r.Participant] = r
		}
		for _, p := range ids {
			st := stats[p]
			r, ok := byP[p]
			placement := last
			if ok && !r.Forfeited {
				placement = r.Placement
				st.Points += r.Points
			}
			st.Played++
			if st.BestPlacement == 0 || placement < st.BestPlacement {
				st.BestPlacement = placement
			}
		}
	}
	lot := lots(g)
	order := ordered(ids, nil,
		func(a, b ParticipantID) int {
			sa, sb := stats[a], stats[b]
			return cmp.Or(sb.Points-sa.Points, sa.BestPlacement-sb.BestPlacement)
		},
		func(a, b ParticipantID) int { return lot[a] - lot[b] })

	plan := Plan{Matches: []PlannedMatch{pm}, Complete: pm.State == Decided}
	plan.Standings = standingsIn(order, stats)
	if plan.Complete {
		plan.Placements = strictPlacements(plan.Standings)
	}
	return plan
}
