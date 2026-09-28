package format

import (
	"fmt"
	"math/bits"
	"slices"
)

// An elimination bracket is a list of nodes in play order. Each node's two
// slots are fed by a seed, or by the winner or loser of an earlier node.

type sourceKind int

const (
	fromSeed sourceKind = iota
	fromWinner
	fromLoser
)

type source struct {
	kind sourceKind
	seed int // 1-based
	key  string
}

type node struct {
	key     string
	round   int
	bracket Bracket
	a, b    source
	// loserLevel is the elimination level of the loser, or 0 when the loser
	// isn't eliminated (an upper-bracket loser drops to the lower bracket).
	loserLevel int
	// forfeitLevel is the elimination level of both Participants after a
	// double Forfeit.
	forfeitLevel int
}

// resolved is what a settled node hands on to the nodes it feeds.
type resolved struct {
	settled bool
	winner  ParticipantID
	loser   ParticipantID
}

func bracketSize(n int) int {
	if n <= 2 {
		return 2
	}
	return 1 << bits.Len(uint(n-1))
}

// bracketOrder lists seeds in bracket position order, so that pairing
// neighbours puts seed 1 against seed S, and the top seeds meet as late as
// possible.
func bracketOrder(size int) []int {
	order := []int{1}
	for len(order) < size {
		next := make([]int, 0, len(order)*2)
		for _, s := range order {
			next = append(next, s, len(order)*2+1-s)
		}
		order = next
	}
	return order
}

func singleEliminationNodes(n int) (nodes []node, winnerLevel int) {
	size := bracketSize(n)
	rounds := bits.Len(uint(size)) - 1
	order := bracketOrder(size)
	for i := 0; i < size/2; i++ {
		nodes = append(nodes, node{
			key: fmt.Sprintf("R1-M%d", i+1), round: 1,
			a:          source{kind: fromSeed, seed: order[2*i]},
			b:          source{kind: fromSeed, seed: order[2*i+1]},
			loserLevel: 1, forfeitLevel: 1,
		})
	}
	for r := 2; r <= rounds; r++ {
		for i := 0; i < size>>r; i++ {
			nodes = append(nodes, node{
				key: fmt.Sprintf("R%d-M%d", r, i+1), round: r,
				a:          source{kind: fromWinner, key: fmt.Sprintf("R%d-M%d", r-1, 2*i+1)},
				b:          source{kind: fromWinner, key: fmt.Sprintf("R%d-M%d", r-1, 2*i+2)},
				loserLevel: r, forfeitLevel: r,
			})
		}
	}
	return nodes, rounds + 1
}

func planElimination(g Group, double bool) Plan {
	n := len(g.Entrants)
	var nodes []node
	var winnerLevel int
	if double {
		nodes, winnerLevel = doubleEliminationNodes(n)
	} else {
		nodes, winnerLevel = singleEliminationNodes(n)
	}
	stored := persisted(g)
	results := map[string]resolved{}
	levels := map[ParticipantID]int{}
	stats := map[ParticipantID]*Standing{}
	for _, e := range g.Entrants {
		stats[e.ID] = &Standing{Participant: e.ID}
	}

	lookup := func(s source) (ParticipantID, bool) {
		switch s.kind {
		case fromSeed:
			if s.seed <= n {
				return g.Entrants[s.seed-1].ID, true
			}
			return "", true
		case fromWinner:
			r := results[s.key]
			return r.winner, r.settled
		default:
			r := results[s.key]
			return r.loser, r.settled
		}
	}

	var plan Plan
	var champion ParticipantID
	settleNode := func(nd node) {
		pa, ka := lookup(nd.a)
		pb, kb := lookup(nd.b)
		pm := PlannedMatch{Key: nd.key, Round: nd.round, Bracket: nd.bracket}
		for _, p := range []ParticipantID{pa, pb} {
			if p != "" {
				pm.Participants = append(pm.Participants, p)
			}
		}
		switch {
		case !ka || !kb:
			pm.State = Pending
		case len(pm.Participants) == 0:
			pm.State = Empty
			results[nd.key] = resolved{settled: true}
		case len(pm.Participants) == 1:
			pm.State = Bye
			pm.Outcome = &Outcome{Winner: pm.Participants[0]}
			results[nd.key] = resolved{settled: true, winner: pm.Participants[0]}
		default:
			pm.State = Playable
			if out, ok := HeadToHeadOutcome(g.Stage.BestOf, pm.Participants, stored[nd.key].Bouts); ok {
				pm.State = Decided
				pm.Outcome = &out
				r := resolved{settled: true, winner: out.Winner}
				if out.DoubleForfeit {
					for _, p := range out.Losers {
						levels[p] = nd.forfeitLevel
					}
				} else {
					r.loser = out.Losers[0]
					if nd.loserLevel > 0 {
						levels[r.loser] = nd.loserLevel
					}
				}
				results[nd.key] = r
				for _, p := range pm.Participants {
					st := stats[p]
					st.Played++
					if p == out.Winner {
						st.Wins++
					} else {
						st.Losses++
					}
				}
			}
		}
		plan.Matches = append(plan.Matches, pm)
	}
	for _, nd := range nodes {
		settleNode(nd)
	}
	last := nodes[len(nodes)-1]
	plan.Complete = results[last.key].settled
	champion = results[last.key].winner
	// Bracket Reset: the lower-bracket finalist beat the undefeated
	// upper-bracket finalist, so both have lost once and they play again.
	if gf := plan.Matches[len(plan.Matches)-1]; double && gf.State == Decided &&
		!gf.Outcome.DoubleForfeit && gf.Outcome.Winner == gf.Participants[1] {
		delete(levels, gf.Participants[0])
		settleNode(node{
			key: "GF-M2", round: last.round + 1, bracket: GrandFinal,
			a:          source{kind: fromLoser, key: last.key},
			b:          source{kind: fromWinner, key: last.key},
			loserLevel: last.loserLevel, forfeitLevel: last.forfeitLevel,
		})
		plan.Complete = results["GF-M2"].settled
		champion = results["GF-M2"].winner
	}
	if plan.Complete && champion != "" {
		levels[champion] = winnerLevel
	}

	seedIndex := map[ParticipantID]int{}
	for i, e := range g.Entrants {
		seedIndex[e.ID] = i
	}
	// Participants still in the bracket rank above everyone eliminated.
	level := func(p ParticipantID) int {
		if l, ok := levels[p]; ok {
			return l
		}
		return winnerLevel + 1
	}
	order := make([]ParticipantID, 0, n)
	for _, e := range g.Entrants {
		order = append(order, e.ID)
	}
	slices.SortStableFunc(order, func(x, y ParticipantID) int {
		if lx, ly := level(x), level(y); lx != ly {
			return ly - lx
		}
		return seedIndex[x] - seedIndex[y]
	})
	dropped := droppedSet(g)
	for i, p := range order {
		st := stats[p]
		st.Rank = i + 1
		st.Dropped = dropped[p]
		_, st.Eliminated = levels[p]
		if st.Eliminated && p == champion {
			st.Eliminated = false
		}
		plan.Standings = append(plan.Standings, *st)
	}
	if plan.Complete {
		plan.Placements = rangesByLevel(order, level)
	}
	return plan
}

// rangesByLevel turns an order sorted by level into Placements where
// Participants of equal level share a range.
func rangesByLevel(order []ParticipantID, level func(ParticipantID) int) []Placement {
	var out []Placement
	for i := 0; i < len(order); {
		j := i
		for j < len(order) && level(order[j]) == level(order[i]) {
			j++
		}
		for _, p := range order[i:j] {
			out = append(out, Placement{Participant: p, From: i + 1, To: j})
		}
		i = j
	}
	return out
}

// doubleEliminationNodes builds an upper bracket like single elimination, a
// lower bracket fed by upper-bracket losers, and a grand final. The lower
// bracket alternates between Rounds where Participants who dropped down meet
// lower-bracket survivors, and Rounds among survivors only.
func doubleEliminationNodes(n int) (nodes []node, winnerLevel int) {
	size := bracketSize(n)
	k := bits.Len(uint(size)) - 1
	lowerRounds := 2 * (k - 1)
	key := func(prefix string, r, i int) string { return fmt.Sprintf("%s%d-M%d", prefix, r, i) }

	// A double Forfeit in the upper bracket knocks both Participants out at
	// the level of the lower-bracket Round they would have dropped into.
	dropLevel := func(upperRound int) int {
		if upperRound == 1 {
			return 1
		}
		return 2 * (upperRound - 1)
	}
	order := bracketOrder(size)
	for i := 0; i < size/2; i++ {
		nodes = append(nodes, node{
			key: key("U", 1, i+1), round: 1, bracket: Upper,
			a:            source{kind: fromSeed, seed: order[2*i]},
			b:            source{kind: fromSeed, seed: order[2*i+1]},
			forfeitLevel: dropLevel(1),
		})
	}
	for r := 2; r <= k; r++ {
		for i := 0; i < size>>r; i++ {
			nodes = append(nodes, node{
				key: key("U", r, i+1), round: r, bracket: Upper,
				a:            source{kind: fromWinner, key: key("U", r-1, 2*i+1)},
				b:            source{kind: fromWinner, key: key("U", r-1, 2*i+2)},
				forfeitLevel: dropLevel(r),
			})
		}
	}
	lower := func(r int, a, b source) node {
		return node{bracket: Lower, round: r, a: a, b: b, loserLevel: r, forfeitLevel: r}
	}
	if k >= 2 {
		for i := 0; i < size/4; i++ {
			nd := lower(1,
				source{kind: fromLoser, key: key("U", 1, 2*i+1)},
				source{kind: fromLoser, key: key("U", 1, 2*i+2)})
			nd.key = key("L", 1, i+1)
			nodes = append(nodes, nd)
		}
	}
	for j := 1; j <= k-1; j++ {
		count := size >> (j + 1)
		for i := 1; i <= count; i++ {
			// Alternate the order Participants drop in, to delay rematches.
			x := i
			if j%2 == 1 {
				x = count + 1 - i
			}
			nd := lower(2*j,
				source{kind: fromWinner, key: key("L", 2*j-1, i)},
				source{kind: fromLoser, key: key("U", j+1, x)})
			nd.key = key("L", 2*j, i)
			nodes = append(nodes, nd)
		}
		if j < k-1 {
			for i := 1; i <= count/2; i++ {
				nd := lower(2*j+1,
					source{kind: fromWinner, key: key("L", 2*j, 2*i-1)},
					source{kind: fromWinner, key: key("L", 2*j, 2*i)})
				nd.key = key("L", 2*j+1, i)
				nodes = append(nodes, nd)
			}
		}
	}
	lowerChampion := source{kind: fromLoser, key: key("U", 1, 1)}
	if lowerRounds > 0 {
		lowerChampion = source{kind: fromWinner, key: key("L", lowerRounds, 1)}
	}
	nodes = append(nodes, node{
		key: "GF-M1", round: k + 1, bracket: GrandFinal,
		a:          source{kind: fromWinner, key: key("U", k, 1)},
		b:          lowerChampion,
		loserLevel: lowerRounds + 1, forfeitLevel: lowerRounds + 1,
	})
	return nodes, lowerRounds + 2
}
