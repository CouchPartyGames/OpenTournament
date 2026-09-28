// Package lifecycle names the statuses a Tournament, its Participants and its
// Matches move through, and how a Match ends. The queries store them and the
// Tournament aggregate moves between them, so they live here, below both,
// where each can import them without a cycle.
package lifecycle

import "github.com/danielgtaylor/huma/v2"

// TournamentStatus is where a Tournament is in its lifecycle: Draft, then
// RegistrationOpen, CheckIn when Check-in is enabled, Running and Completed.
// It can be Cancelled at any point before Completed.
type TournamentStatus string

// Tournament statuses.
const (
	Draft            TournamentStatus = "draft"
	RegistrationOpen TournamentStatus = "registration-open"
	CheckIn          TournamentStatus = "check-in"
	Running          TournamentStatus = "running"
	Completed        TournamentStatus = "completed"
	Cancelled        TournamentStatus = "cancelled"
)

// Schema lists the Tournament statuses as an enum in the OpenAPI document,
// which also rejects any other status in a request.
func (TournamentStatus) Schema(huma.Registry) *huma.Schema {
	return enum(Draft, RegistrationOpen, CheckIn, Running, Completed, Cancelled)
}

// ParticipantStatus is where a Participant is in a Tournament: Registered,
// then CheckedIn when Check-in is enabled, or NotCheckedIn if they miss it.
// Everyone still in at the start is Active until they leave through a
// Withdrawal or Disqualification, or are Eliminated when they don't advance.
type ParticipantStatus string

// Participant statuses.
const (
	Registered   ParticipantStatus = "registered"
	CheckedIn    ParticipantStatus = "checked-in"
	NotCheckedIn ParticipantStatus = "not-checked-in"
	Active       ParticipantStatus = "active"
	Withdrawn    ParticipantStatus = "withdrawn"
	Disqualified ParticipantStatus = "disqualified"
	Eliminated   ParticipantStatus = "eliminated"
)

// HasLeft reports whether the Participant left the Tournament through a
// Withdrawal or Disqualification.
func (s ParticipantStatus) HasLeft() bool { return s == Withdrawn || s == Disqualified }

// Schema lists the Participant statuses as an enum in the OpenAPI document.
func (ParticipantStatus) Schema(huma.Registry) *huma.Schema {
	return enum(Registered, CheckedIn, NotCheckedIn, Active, Withdrawn, Disqualified, Eliminated)
}

// MatchStatus is where a Match is in its lifecycle: Pending until its
// Participants are known and its Round opens, then Ready, Allocating while it
// gets a Game Server and InProgress once that server reports it started,
// until it is Completed. An Abort sends it back to Allocating, and a Bye or
// an empty Match goes straight from Pending to Completed.
//
// A Match with no result by its Result Deadline is Stalled until the
// Organizer resolves it, or its Game Server reports the deciding result late
// while its token is still good. One still unfinished when its Tournament is
// cancelled is Cancelled.
type MatchStatus string

// Match statuses. Unlike the Tournament and Participant statuses they carry a
// prefix, as several share a name with those.
const (
	MatchPending    MatchStatus = "pending"
	MatchReady      MatchStatus = "ready"
	MatchAllocating MatchStatus = "allocating"
	MatchInProgress MatchStatus = "in-progress"
	MatchStalled    MatchStatus = "stalled"
	MatchCompleted  MatchStatus = "completed"
	MatchCancelled  MatchStatus = "cancelled"
)

// Open reports whether the Match can still receive results.
func (s MatchStatus) Open() bool { return s == MatchReady || s.Playing() || s == MatchStalled }

// Playing reports whether the Match is being played, or about to be, on a
// Game Server of its own: Allocating or InProgress.
func (s MatchStatus) Playing() bool { return s == MatchAllocating || s == MatchInProgress }

// Schema lists the Match statuses as an enum in the OpenAPI document.
func (MatchStatus) Schema(huma.Registry) *huma.Schema {
	return enum(MatchPending, MatchReady, MatchAllocating, MatchInProgress, MatchStalled, MatchCompleted, MatchCancelled)
}

// MatchResult is how a Completed Match ended: a Win, a double Forfeit, a Bye,
// Empty when it had no Participants at all, or FreeForAll for any other
// Match of a free-for-all Stage, whose Standings its Bouts decide.
type MatchResult string

// Match results.
const (
	ResultWin           MatchResult = "win"
	ResultDoubleForfeit MatchResult = "double-forfeit"
	ResultBye           MatchResult = "bye"
	ResultEmpty         MatchResult = "empty"
	ResultFreeForAll    MatchResult = "free-for-all"
)

// Schema lists the Match results as an enum in the OpenAPI document.
func (MatchResult) Schema(huma.Registry) *huma.Schema {
	return enum(ResultWin, ResultDoubleForfeit, ResultBye, ResultEmpty, ResultFreeForAll)
}

// enum describes a string type that holds only the given values.
func enum[T ~string](values ...T) *huma.Schema {
	s := &huma.Schema{Type: huma.TypeString}
	for _, v := range values {
		s.Enum = append(s.Enum, string(v))
	}
	return s
}
