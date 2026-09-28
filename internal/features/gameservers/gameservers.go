// Package gameservers is the slice Game Servers talk to, authenticated with
// the per-Match token they received on allocation (ADR-0002). Only Game
// Servers report results.
package gameservers

import (
	"context"
	"net/http"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
)

// PathPrefix is where the Game Server endpoints live. Keycloak tokens are
// not accepted here.
const PathPrefix = "/api/v1/game-server"

var matchToken = []map[string][]string{{"matchToken": {}}}

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc}
	problem.Register(api, huma.Operation{
		OperationID: "game-server-get-match", Method: http.MethodGet, Path: PathPrefix + "/match",
		Summary: "Get my Match", Tags: []string{"Game Servers"}, Security: matchToken,
		Description: "Participants and their Player Identities, the Best-of or Bout count, Bouts already completed " +
			"(to resume after an Abort) and which Participants have forfeited.",
	}, h.get)
	problem.Register(api, huma.Operation{
		OperationID: "game-server-report-started", Method: http.MethodPost, Path: PathPrefix + "/match/started",
		Summary: "Report the Match started", Tags: []string{"Game Servers"}, Security: matchToken,
		DefaultStatus: http.StatusNoContent,
	}, h.started)
	problem.Register(api, huma.Operation{
		OperationID: "game-server-report-bout", Method: http.MethodPut, Path: PathPrefix + "/match/bouts/{bout}",
		Summary: "Report a Bout result", Tags: []string{"Game Servers"}, Security: matchToken,
		DefaultStatus: http.StatusNoContent,
		Description: "Head-to-head: the winner. Free-for-all: a strict placement and points for every Participant still playing. " +
			"Repeating an identical report is harmless; a different one conflicts. A Bout already forfeited can't be reported.",
	}, h.bout)
	problem.Register(api, huma.Operation{
		OperationID: "game-server-report-no-shows", Method: http.MethodPost, Path: PathPrefix + "/match/bouts/{bout}/no-shows",
		Summary: "Report No-shows for a Bout", Tags: []string{"Game Servers"}, Security: matchToken,
		DefaultStatus: http.StatusNoContent,
		Description:   "Each No-show forfeits the Bout. Both head-to-head Participants missing is a double Forfeit.",
	}, h.noShows)
}

type handlers struct{ svc *tournament.Service }

// MatchToken carries a Game Server's per-Match token.
type MatchToken struct {
	Authorization string `header:"Authorization" doc:"Bearer <match token>"`
}

type boutInput struct {
	MatchToken
	Bout int32 `path:"bout" minimum:"1"`
	Body struct {
		Winner     *ids.ParticipantID `json:"winner,omitempty" doc:"Head-to-head: the Participant who won the Bout"`
		Placements []BoutPlacement    `json:"placements,omitempty" doc:"Free-for-all: every Participant still playing"`
	}
}

// BoutPlacement is one Participant's free-for-all result.
type BoutPlacement struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	Placement     int32             `json:"placement" minimum:"1"`
	Points        int32             `json:"points" doc:"Computed by the Game Server; the platform only sums them"`
}

type noShowInput struct {
	MatchToken
	Bout int32 `path:"bout" minimum:"1"`
	Body struct {
		ParticipantIDs []ids.ParticipantID `json:"participantIds" minItems:"1" uniqueItems:"true"`
	}
}

type matchOutput struct {
	Body tournament.ServerMatchView
}

func (h handlers) as(ctx context.Context, in MatchToken, fn func(tx *tournament.Tx, m db.Match) error) error {
	token, _ := auth.BearerToken(in.Authorization)
	return h.svc.AsGameServer(ctx, token, fn)
}

func (h handlers) get(ctx context.Context, in *MatchToken) (*matchOutput, error) {
	out := &matchOutput{}
	return out, h.as(ctx, *in, func(tx *tournament.Tx, m db.Match) (err error) {
		out.Body, err = tx.ServerMatch(m)
		return err
	})
}

func (h handlers) started(ctx context.Context, in *MatchToken) (*struct{}, error) {
	return nil, h.as(ctx, *in, func(tx *tournament.Tx, m db.Match) error { return tx.ReportStarted(m) })
}

func (h handlers) bout(ctx context.Context, in *boutInput) (*struct{}, error) {
	r := tournament.BoutReport{Bout: in.Bout}
	if in.Body.Winner != nil {
		r.Winner = *in.Body.Winner
	}
	for _, p := range in.Body.Placements {
		r.Placements = append(r.Placements, tournament.PlacementReport{ParticipantID: p.ParticipantID, Placement: p.Placement, Points: p.Points})
	}
	if (in.Body.Winner == nil) == (len(in.Body.Placements) == 0) {
		return nil, problem.Fields{{Location: "body", Message: "give a winner (head-to-head) or placements (free-for-all)"}}.Err()
	}
	return nil, h.as(ctx, in.MatchToken, func(tx *tournament.Tx, m db.Match) error { return tx.ReportBout(m, r) })
}

func (h handlers) noShows(ctx context.Context, in *noShowInput) (*struct{}, error) {
	return nil, h.as(ctx, in.MatchToken, func(tx *tournament.Tx, m db.Match) error {
		return tx.ReportNoShows(m, in.Bout, in.Body.ParticipantIDs)
	})
}
