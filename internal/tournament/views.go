package tournament

import (
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/ids"
)

// StandingView is a Standing as the API shows it.
type StandingView struct {
	ParticipantID    ids.ParticipantID `json:"participantId"`
	Position         int               `json:"position" doc:"1-based position in the Group"`
	Played           int               `json:"played"`
	Wins             int               `json:"wins"`
	Losses           int               `json:"losses"`
	Points           int               `json:"points"`
	Buchholz         int               `json:"buchholz,omitempty" doc:"Swiss only"`
	BoutDifferential int               `json:"boutDifferential,omitempty" doc:"Round robin only"`
	BestPlacement    int               `json:"bestPlacement,omitempty" doc:"Free-for-all only"`
	Eliminated       bool              `json:"eliminated,omitempty" doc:"Elimination formats only"`
	Dropped          bool              `json:"dropped,omitempty" doc:"Withdrew or was disqualified"`
}

// StandingsView converts engine Standings.
func StandingsView(ss []format.Standing) []StandingView {
	out := make([]StandingView, len(ss))
	for i, s := range ss {
		out[i] = StandingView{
			ParticipantID: ParticipantOf(s.Participant), Position: s.Position, Played: s.Played,
			Wins: s.Wins, Losses: s.Losses, Points: s.Points, Buchholz: s.Buchholz,
			BoutDifferential: s.BoutDifferential, BestPlacement: s.BestPlacement,
			Eliminated: s.Eliminated, Dropped: s.Dropped,
		}
	}
	return out
}

// PlacementView is a Final Placement range, e.g. 5th–8th.
type PlacementView struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	From          int               `json:"from" doc:"Best place of the shared range"`
	To            int               `json:"to" doc:"Worst place of the shared range"`
}
