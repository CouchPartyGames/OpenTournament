// Package format is the Format engine: the pure rules that decide, for one
// Group of a Stage, which Matches exist, who plays in them, when they are
// playable, the Standings, Group completion and Placements.
//
// It imports nothing that touches the database, network or clock. The same
// inputs always produce the same outputs: randomness comes in through each
// Participant's Lot, drawn by the caller.
//
// The engine is level-triggered: callers pass the Group's persisted Matches
// and Bouts, and it returns the complete plan for the Group as it stands. A
// Match the plan contains that the caller hasn't persisted yet is a Match to
// create.
package format

import (
	"errors"
	"fmt"
)

// ParticipantID identifies a Participant inside the engine.
type ParticipantID string

// Kind is a Stage's Format.
type Kind string

const (
	SingleElimination Kind = "single-elimination"
	DoubleElimination Kind = "double-elimination"
	RoundRobin        Kind = "round-robin"
	Swiss             Kind = "swiss"
	FreeForAll        Kind = "free-for-all"
)

// Kinds lists every Format.
var Kinds = []Kind{SingleElimination, DoubleElimination, RoundRobin, Swiss, FreeForAll}

// HeadToHead reports whether Matches of this Format are between two Participants.
func (k Kind) HeadToHead() bool { return k != FreeForAll }

// Elimination reports whether the Format knocks Participants out.
func (k Kind) Elimination() bool { return k == SingleElimination || k == DoubleElimination }

// Stage is the part of a Stage definition that the Format engine needs.
type Stage struct {
	Format Kind
	// BestOf is 1 or 3 for head-to-head Formats.
	BestOf int
	// Bouts is the number of Bouts of a free-for-all Match.
	Bouts int
	// SwissRounds overrides the default number of Swiss Rounds when positive.
	SwissRounds int
}

// Participant is a Participant placed in a Group.
type Participant struct {
	ID ParticipantID
	// Lot is the Participant's random draw, used as the last Tiebreaker. Lower wins.
	Lot int
}

// Group is the persisted state of one Group.
type Group struct {
	Stage Stage
	// Participants are in Seeding order: the first is the top seed.
	Participants []Participant
	// Matches are the Group's persisted Matches with their Bouts so far.
	Matches []Match
	// Dropped are the Participants who withdrew or were disqualified. They are
	// left out of future Swiss pairings and Advancement.
	Dropped []ParticipantID
}

// Match is a persisted Match.
type Match struct {
	Key          string
	Round        int
	Participants []ParticipantID
	Bouts        []Bout
}

// Bout is one recorded Bout of a Match.
type Bout struct {
	Number  int
	Results []BoutResult
}

// BoutResult is one Participant's result in a Bout.
type BoutResult struct {
	Participant ParticipantID
	// Won is set for the winner of a head-to-head Bout.
	Won bool
	// Forfeited is set when the Participant did not complete the Bout.
	Forfeited bool
	// Placement and Points are the free-for-all result.
	Placement int
	Points    int
}

// MatchState is the engine's view of a Match.
type MatchState string

const (
	// Pending: not every Participant is known yet, or its Round hasn't opened.
	Pending MatchState = "pending"
	// Playable: every Participant is known and it can be played now.
	Playable MatchState = "playable"
	// Decided: its Bouts decide it.
	Decided MatchState = "decided"
	// Bye: it has a single Participant, who advances without playing.
	Bye MatchState = "bye"
	// Empty: it has no Participant at all, for example after a double Forfeit fed it.
	Empty MatchState = "empty"
)

// Settled reports whether nothing more will happen in the Match.
func (s MatchState) Settled() bool { return s == Decided || s == Bye || s == Empty }

// Bracket names the part of a double-elimination Stage a Match belongs to.
type Bracket string

const (
	NoBracket  Bracket = ""
	Upper      Bracket = "upper"
	Lower      Bracket = "lower"
	GrandFinal Bracket = "grand-final"
)

// PlannedMatch is a Match as the engine plans it.
type PlannedMatch struct {
	Key     string
	Round   int
	Bracket Bracket
	// Participants are the Participants known so far, in slot order. A
	// Pending elimination Match may list fewer than it will hold.
	Participants []ParticipantID
	State        MatchState
	// Outcome is set when the State is Decided or Bye.
	Outcome *Outcome
}

// Outcome is how a Match ended.
type Outcome struct {
	// Winner is empty after a double Forfeit, and for free-for-all Matches.
	Winner        ParticipantID
	Losers        []ParticipantID
	DoubleForfeit bool
}

// Standing is a Participant's position in a Group.
type Standing struct {
	Participant ParticipantID
	// Position is 1-based and strict.
	Position int
	Played   int
	Wins     int
	Losses   int
	Points   int
	Dropped  bool
	// Buchholz is set for Swiss.
	Buchholz int
	// BoutDifferential is set for round robin.
	BoutDifferential int
	// BestPlacement is set for free-for-all.
	BestPlacement int
	// Eliminated is set for elimination Formats.
	Eliminated bool
}

// Placement is a Participant's finishing range, e.g. 5th–8th.
type Placement struct {
	Participant ParticipantID
	From, To    int
}

// Plan is everything the engine decides about a Group.
type Plan struct {
	Matches   []PlannedMatch
	Standings []Standing
	Complete  bool
	// Placements are set once the Group is Complete, best first.
	Placements []Placement
}

// ErrInvalidGroup is returned when a Group can't be planned.
var ErrInvalidGroup = errors.New("invalid group")

// PlanGroup returns the plan for a Group given its persisted state.
func PlanGroup(g Group) (Plan, error) {
	if err := validate(g); err != nil {
		return Plan{}, err
	}
	switch g.Stage.Format {
	case SingleElimination:
		return planElimination(g, false), nil
	case DoubleElimination:
		return planElimination(g, true), nil
	case RoundRobin:
		return planRoundRobin(g), nil
	case Swiss:
		return planSwiss(g), nil
	case FreeForAll:
		return planFreeForAll(g), nil
	}
	return Plan{}, fmt.Errorf("%w: unknown format %q", ErrInvalidGroup, g.Stage.Format)
}

func validate(g Group) error {
	if g.Stage.Format.HeadToHead() && g.Stage.BestOf != 1 && g.Stage.BestOf != 3 {
		return fmt.Errorf("%w: best-of must be 1 or 3, got %d", ErrInvalidGroup, g.Stage.BestOf)
	}
	if g.Stage.Format == FreeForAll && g.Stage.Bouts < 1 {
		return fmt.Errorf("%w: a free-for-all match needs at least one bout", ErrInvalidGroup)
	}
	seen := map[ParticipantID]bool{}
	for _, e := range g.Participants {
		if e.ID == "" || seen[e.ID] {
			return fmt.Errorf("%w: entrant %q is empty or duplicated", ErrInvalidGroup, e.ID)
		}
		seen[e.ID] = true
	}
	return nil
}

// persisted indexes a Group's persisted Matches by key.
func persisted(g Group) map[string]Match {
	out := make(map[string]Match, len(g.Matches))
	for _, m := range g.Matches {
		out[m.Key] = m
	}
	return out
}

func droppedSet(g Group) map[ParticipantID]bool {
	out := make(map[ParticipantID]bool, len(g.Dropped))
	for _, p := range g.Dropped {
		out[p] = true
	}
	return out
}
