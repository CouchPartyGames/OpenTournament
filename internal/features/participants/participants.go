// Package participants is the slice for leaving a running Tournament: a
// Participant's Withdrawal, or the Organizer's Disqualification.
package participants

import (
	"context"
	"net/http"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
)

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc}
	problem.Register(api, huma.Operation{
		OperationID: "withdraw", Method: http.MethodPost, Path: "/api/v1/tournaments/{tournamentId}/participants/{participantId}/withdraw",
		Summary: "Withdraw from a running Tournament", Tags: []string{"Participants"}, Security: auth.Security(),
		DefaultStatus: http.StatusNoContent,
		Description:   "The Participant forfeits every Bout they haven't completed, including in a Match already In Progress.",
	}, h.withdraw)
	problem.Register(api, huma.Operation{
		OperationID: "disqualify", Method: http.MethodPost, Path: "/api/v1/tournaments/{tournamentId}/participants/{participantId}/disqualify",
		Summary: "Disqualify a Participant", Tags: []string{"Participants"}, Security: auth.Security(),
		DefaultStatus: http.StatusNoContent,
		Description:   "The Organizer removes a Participant, who forfeits every Bout they haven't completed.",
	}, h.disqualify)
}

type handlers struct{ svc *tournament.Service }

type input struct {
	TournamentID  ids.TournamentID  `path:"tournamentId"`
	ParticipantID ids.ParticipantID `path:"participantId"`
}

func (h handlers) withdraw(ctx context.Context, in *input) (*struct{}, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	return nil, Withdraw(ctx, h.svc, p, in.TournamentID, in.ParticipantID)
}

func (h handlers) disqualify(ctx context.Context, in *input) (*struct{}, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	return nil, Disqualify(ctx, h.svc, p, in.TournamentID, in.ParticipantID)
}

// Withdraw lets a Participant leave of their own accord.
func Withdraw(ctx context.Context, svc *tournament.Service, p auth.Principal, tid ids.TournamentID, pid ids.ParticipantID) error {
	return svc.InTournament(ctx, tid, func(tx *tournament.Tx) error {
		person, err := tx.Participant(pid)
		if err != nil {
			return err
		}
		if !p.Owns(auth.Identity{Kind: person.IdentityKind, Value: person.IdentityValue}) {
			return problem.New(problem.Forbidden, "identity-not-owned", "you can only withdraw yourself")
		}
		return tx.Leave(pid, tournament.Withdrawn)
	})
}

// Disqualify lets the Organizer remove a Participant.
func Disqualify(ctx context.Context, svc *tournament.Service, p auth.Principal, tid ids.TournamentID, pid ids.ParticipantID) error {
	return svc.InTournament(ctx, tid, func(tx *tournament.Tx) error {
		if err := tx.RequireOrganizer(p); err != nil {
			return err
		}
		return tx.Leave(pid, tournament.Disqualified)
	})
}
