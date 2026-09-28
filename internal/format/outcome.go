package format

// HeadToHeadOutcome decides a head-to-head Match from its Bouts. It reports
// false while the Match is still undecided.
//
// A Bout forfeited by both Participants is a double Forfeit and decides the
// Match at once: both lose. Otherwise the first Participant to win the majority
// of the Best-of wins.
func HeadToHeadOutcome(bestOf int, participants []ParticipantID, bouts []Bout) (Outcome, bool) {
	if len(participants) != 2 {
		return Outcome{}, false
	}
	need := bestOf/2 + 1
	wins := map[ParticipantID]int{}
	for _, b := range bouts {
		forfeits := 0
		for _, r := range b.Results {
			if r.Forfeited {
				forfeits++
			}
			if r.Won {
				wins[r.Participant]++
			}
		}
		if forfeits >= 2 {
			return Outcome{Losers: participants, DoubleForfeit: true}, true
		}
		for i, p := range participants {
			if wins[p] >= need {
				return Outcome{Winner: p, Losers: []ParticipantID{participants[1-i]}}, true
			}
		}
	}
	return Outcome{}, false
}

// BoutsWon counts the Bouts each Participant won.
func BoutsWon(bouts []Bout) map[ParticipantID]int {
	out := map[ParticipantID]int{}
	for _, b := range bouts {
		for _, r := range b.Results {
			if r.Won {
				out[r.Participant]++
			}
		}
	}
	return out
}
