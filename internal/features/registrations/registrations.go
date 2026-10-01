// Package registrations is the slice for registering, unregistering and
// checking in before a Tournament starts.
package registrations

import (
	"context"
	"errors"
	"net/http"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

// Error codes.
const (
	CodeRegistrationClosed = "registration-closed"
	CodeTournamentFull     = "tournament-full"
	CodeAlreadyRegistered  = "already-registered"
	CodeKindNotAccepted    = "identity-kind-not-accepted"
	CodeIdentityNotOwned   = "identity-not-owned"
	CodeCheckInClosed      = "check-in-closed"
)

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc}
	problem.Register(api, huma.Operation{
		OperationID: "register", Method: http.MethodPost, Path: "/api/v1/tournaments/{tournamentId}/participants",
		Summary: "Register a Participant", Tags: []string{"Registrations"}, DefaultStatus: http.StatusCreated, Security: auth.Security(),
		Description: "A person registers their own Keycloak identity (the default), or a linked identity exposed as a claim. " +
			"A trusted Game backend can register any Player Identity of a kind the Game accepts. First come, first served " +
			"up to the Capacity, only during the Registration Window. Registering during the Check-in Window checks in too.",
	}, h.register)
	problem.Register(api, huma.Operation{
		OperationID: "unregister", Method: http.MethodDelete, Path: "/api/v1/tournaments/{tournamentId}/participants/{participantId}",
		Summary: "Unregister before the start", Tags: []string{"Registrations"}, Security: auth.Security(),
	}, h.unregister)
	problem.Register(api, huma.Operation{
		OperationID: "check-in", Method: http.MethodPost, Path: "/api/v1/tournaments/{tournamentId}/participants/{participantId}/check-in",
		Summary: "Check in during the Check-in Window", Tags: []string{"Registrations"}, Security: auth.Security(),
	}, h.checkIn)
	problem.Register(api, huma.Operation{
		OperationID: "list-participants", Method: http.MethodGet, Path: "/api/v1/tournaments/{tournamentId}/participants",
		Summary: "List Participants", Tags: []string{"Registrations"},
	}, h.list)
	problem.Register(api, huma.Operation{
		OperationID: "my-registrations", Method: http.MethodGet, Path: "/api/v1/tournaments/{tournamentId}/participants/me",
		Summary: "See my registration", Tags: []string{"Registrations"}, Security: auth.Security(),
		Description: "The caller's Participants in the Tournament, one per Player Identity they own: whether they are registered and checked in.",
	}, h.me)
}

type handlers struct{ svc *tournament.Service }

type registerInput struct {
	TournamentID ids.TournamentID `path:"tournamentId"`
	Body         struct {
		Identity *auth.Identity `json:"identity,omitempty" doc:"Defaults to the caller's own Keycloak identity"`
	}
}

type participantInput struct {
	TournamentID  ids.TournamentID  `path:"tournamentId"`
	ParticipantID ids.ParticipantID `path:"participantId"`
}

type tournamentInput struct {
	TournamentID ids.TournamentID `path:"tournamentId"`
}

type participantOutput struct{ Body ParticipantView }

// ParticipantRegistrations lists a caller's or a Tournament's Participants.
// A named body prevents schema-name collisions with other slices' list outputs.
type ParticipantRegistrations struct {
	Participants []ParticipantView `json:"participants"`
}

type listOutput struct {
	Body ParticipantRegistrations
}

func (h handlers) register(ctx context.Context, in *registerInput) (*participantOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	v, err := Enter(ctx, h.svc, p, in.TournamentID, in.Body.Identity)
	return &participantOutput{v}, err
}

func (h handlers) unregister(ctx context.Context, in *participantInput) (*struct{}, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	return nil, Unregister(ctx, h.svc, p, in.TournamentID, in.ParticipantID)
}

func (h handlers) checkIn(ctx context.Context, in *participantInput) (*participantOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	v, err := CheckIn(ctx, h.svc, p, in.TournamentID, in.ParticipantID)
	return &participantOutput{v}, err
}

func (h handlers) list(ctx context.Context, in *tournamentInput) (*listOutput, error) {
	vs, err := List(ctx, h.svc.Queries, in.TournamentID, nil)
	out := &listOutput{}
	out.Body.Participants = vs
	return out, err
}

func (h handlers) me(ctx context.Context, in *tournamentInput) (*listOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	vs, err := List(ctx, h.svc.Queries, in.TournamentID, &p)
	out := &listOutput{}
	out.Body.Participants = vs
	return out, err
}

// mayActFor reports whether the caller may register or check in a Player
// Identity: their own, or any as a Game backend the Game trusts.
func mayActFor(p auth.Principal, game games.Game, id auth.Identity) error {
	if p.IsService() {
		if game.Trusts(p.ClientID) {
			return nil
		}
		return problem.New(problem.Forbidden, "not-trusted-for-game", "client %q is not trusted to act for game %q", p.ClientID, game.ID)
	}
	if p.Owns(id) {
		return nil
	}
	return problem.New(problem.Forbidden, CodeIdentityNotOwned, "you can only act for your own player identity")
}

// Enter registers a Participant.
func Enter(ctx context.Context, svc *tournament.Service, p auth.Principal, tid ids.TournamentID, identity *auth.Identity) (ParticipantView, error) {
	var id auth.Identity
	switch {
	case identity != nil:
		id = *identity
	case p.IsService():
		return ParticipantView{}, problem.Fields{{Location: "body.identity", Message: "a game backend must say which player identity to register"}}.Err()
	default:
		id = auth.Identity{Kind: games.KeycloakIdentity, Value: p.Subject}
	}
	var v ParticipantView
	err := svc.InTournament(ctx, tid, func(tx *tournament.Tx) error {
		game, err := tx.Game()
		if err != nil {
			return err
		}
		if !game.Accepts(id.Kind) {
			return problem.New(problem.Invalid, CodeKindNotAccepted, "game %q doesn't accept %q identities", game.ID, id.Kind)
		}
		if err := mayActFor(p, game, id); err != nil {
			return err
		}
		if tx.T.Status != lifecycle.RegistrationOpen && tx.T.Status != lifecycle.CheckIn {
			return problem.New(problem.Conflict, CodeRegistrationClosed, "registration is not open")
		}
		count, err := tx.Q.CountRegistered(tx.Ctx(), tid)
		if err != nil {
			return err
		}
		if count >= tx.T.Capacity {
			return problem.New(problem.Conflict, CodeTournamentFull, "the tournament is full")
		}
		row := db.Participant{
			ID: ids.New[ids.ParticipantID](), TournamentID: tid, IdentityKind: id.Kind, IdentityValue: id.Value,
			RegisteredBy: p.ID(), Status: lifecycle.Registered, RegisteredAt: tx.Now(),
		}
		// Registering inside the Check-in Window counts as checking in.
		if tx.T.Status == lifecycle.CheckIn {
			now := tx.Now()
			row.Status, row.CheckedInAt = lifecycle.CheckedIn, &now
		}
		err = tx.Q.InsertParticipant(tx.Ctx(), db.InsertParticipantParams{
			ID: row.ID, TournamentID: tid, IdentityKind: row.IdentityKind, IdentityValue: row.IdentityValue,
			RegisteredBy: row.RegisteredBy, Status: row.Status, RegisteredAt: row.RegisteredAt, CheckedInAt: row.CheckedInAt,
		})
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return problem.New(problem.Conflict, CodeAlreadyRegistered, "this player identity is already registered")
		} else if err != nil {
			return err
		}
		v = view(row)
		return emitCounts(tx)
	})
	return v, err
}

// Unregister frees a spot before the start.
func Unregister(ctx context.Context, svc *tournament.Service, p auth.Principal, tid ids.TournamentID, pid ids.ParticipantID) error {
	return svc.InTournament(ctx, tid, func(tx *tournament.Tx) error {
		person, err := own(tx, p, pid)
		if err != nil {
			return err
		}
		if tx.T.Status != lifecycle.RegistrationOpen && tx.T.Status != lifecycle.CheckIn {
			return problem.New(problem.Conflict, CodeRegistrationClosed, "participants can only unregister before the start")
		}
		if err := tx.Q.DeleteParticipant(tx.Ctx(), person.ID); err != nil {
			return err
		}
		return emitCounts(tx)
	})
}

// CheckIn confirms a registered Participant is present.
func CheckIn(ctx context.Context, svc *tournament.Service, p auth.Principal, tid ids.TournamentID, pid ids.ParticipantID) (ParticipantView, error) {
	var v ParticipantView
	err := svc.InTournament(ctx, tid, func(tx *tournament.Tx) error {
		person, err := own(tx, p, pid)
		if err != nil {
			return err
		}
		if tx.T.Status != lifecycle.CheckIn {
			return problem.New(problem.Conflict, CodeCheckInClosed, "check-in is not open")
		}
		if person.CheckedInAt == nil {
			now := tx.Now()
			if err := tx.Q.CheckInParticipant(tx.Ctx(), db.CheckInParticipantParams{ID: pid, CheckedInAt: &now}); err != nil {
				return err
			}
			person.Status, person.CheckedInAt = lifecycle.CheckedIn, &now
			if err := emitCounts(tx); err != nil {
				return err
			}
		}
		v = view(person)
		return nil
	})
	return v, err
}

// own loads a Participant the caller may act for.
func own(tx *tournament.Tx, p auth.Principal, pid ids.ParticipantID) (db.Participant, error) {
	person, err := tx.Participant(pid)
	if err != nil {
		return db.Participant{}, err
	}
	game, err := tx.Game()
	if err != nil {
		return db.Participant{}, err
	}
	return person, mayActFor(p, game, auth.Identity{Kind: person.IdentityKind, Value: person.IdentityValue})
}

func emitCounts(tx *tournament.Tx) error {
	counts, err := tx.Q.RegistrationCounts(tx.Ctx(), ids.UUIDs([]ids.TournamentID{tx.T.ID}))
	if err != nil {
		return err
	}
	var registered, checkedIn int32
	if len(counts) == 1 {
		registered, checkedIn = counts[0].Registered, counts[0].CheckedIn
	}
	return tx.Emit(events.RegistrationsChanged, map[string]any{"registered": registered, "checkedIn": checkedIn})
}

// List reads a Tournament's Participants, or only the caller's.
func List(ctx context.Context, q *db.Queries, tid ids.TournamentID, mine *auth.Principal) ([]ParticipantView, error) {
	if _, err := q.GetTournament(ctx, tid); errors.Is(err, pgx.ErrNoRows) {
		return nil, tournament.ErrTournamentNotFound
	} else if err != nil {
		return nil, err
	}
	ps, err := q.ListParticipants(ctx, tid)
	if err != nil {
		return nil, err
	}
	out := []ParticipantView{}
	for _, p := range ps {
		if mine != nil && !mine.Owns(auth.Identity{Kind: p.IdentityKind, Value: p.IdentityValue}) {
			continue
		}
		out = append(out, view(p))
	}
	return out, nil
}

// ParticipantView is a Participant as the API shows it.
type ParticipantView struct {
	ID           ids.ParticipantID           `json:"id"`
	Identity     auth.Identity               `json:"identity"`
	Status       lifecycle.ParticipantStatus `json:"status"`
	RegisteredAt time.Time                   `json:"registeredAt"`
	CheckedInAt  *time.Time                  `json:"checkedInAt,omitempty"`
}

func view(p db.Participant) ParticipantView {
	return ParticipantView{
		ID: p.ID, Identity: auth.Identity{Kind: p.IdentityKind, Value: p.IdentityValue},
		Status: p.Status, RegisteredAt: p.RegisteredAt, CheckedInAt: p.CheckedInAt,
	}
}
