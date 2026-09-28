package format

import "fmt"

// roundRobinRounds pairs everyone with everyone once using the circle method.
// With an odd count, one Participant sits out each Round.
func roundRobinRounds(ps []ParticipantID) [][][2]ParticipantID {
	arr := append([]ParticipantID(nil), ps...)
	if len(arr)%2 == 1 {
		arr = append(arr, "")
	}
	n := len(arr)
	var rounds [][][2]ParticipantID
	for r := 0; r < n-1; r++ {
		var pairs [][2]ParticipantID
		for i := 0; i < n/2; i++ {
			a, b := arr[i], arr[n-1-i]
			if a == "" || b == "" {
				continue
			}
			if r%2 == 1 && i == 0 {
				a, b = b, a
			}
			pairs = append(pairs, [2]ParticipantID{a, b})
		}
		rounds = append(rounds, pairs)
		arr = append([]ParticipantID{arr[0], arr[n-1]}, arr[1:n-1]...)
	}
	return rounds
}

func planRoundRobin(g Group) Plan {
	stored := persisted(g)
	rec := newRecord(g)
	var plan Plan
	open := true // every earlier Round is settled
	plan.Complete = true
	for r, pairs := range roundRobinRounds(participantIDs(g)) {
		roundSettled := true
		for i, pair := range pairs {
			pm := PlannedMatch{
				Key:          fmt.Sprintf("R%d-M%d", r+1, i+1),
				Round:        r + 1,
				Participants: []ParticipantID{pair[0], pair[1]},
				State:        Pending,
			}
			if open {
				pm.State = Playable
				bouts := stored[pm.Key].Bouts
				if out, ok := HeadToHeadOutcome(g.Stage.BestOf, pm.Participants, bouts); ok {
					pm.State = Decided
					pm.Outcome = &out
					rec.add(pm, bouts)
				}
			}
			if pm.State != Decided {
				roundSettled = false
				plan.Complete = false
			}
			plan.Matches = append(plan.Matches, pm)
		}
		open = open && roundSettled
	}
	lot := lots(g)
	order := ordered(participantIDs(g), rec.beat,
		func(a, b ParticipantID) int { return rec.stats[b].Points - rec.stats[a].Points },
		func(a, b ParticipantID) int {
			if d := rec.stats[b].BoutDifferential - rec.stats[a].BoutDifferential; d != 0 {
				return d
			}
			return lot[a] - lot[b]
		})
	plan.Standings = standingsIn(order, rec.stats)
	if plan.Complete {
		plan.Placements = strictPlacements(plan.Standings)
	}
	return plan
}
