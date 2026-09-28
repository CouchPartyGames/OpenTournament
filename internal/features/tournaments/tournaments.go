// Package tournaments is the slice for creating, configuring, reading and
// cancelling Tournaments.
package tournaments

import (
	"context"
	"errors"
	"net/http"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/jackc/pgx/v5"
)

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc}
	problem.Register(api, huma.Operation{
		OperationID: "create-tournament", Method: http.MethodPost, Path: "/api/v1/tournaments",
		Summary: "Create a Tournament", Tags: []string{"Tournaments"}, DefaultStatus: http.StatusCreated,
		Description: "Any authenticated user can create a Tournament and becomes its Organizer. A Game backend " +
			"can create Tournaments for the Games that trust its client.",
		Security: auth.Security(),
	}, h.create)
	problem.Register(api, huma.Operation{
		OperationID: "edit-tournament", Method: http.MethodPut, Path: "/api/v1/tournaments/{tournamentId}",
		Summary: "Edit a draft Tournament", Tags: []string{"Tournaments"}, Security: auth.Security(),
		Description: "TournamentSettings can only change while the Tournament is a Draft; they freeze once registration opens.",
	}, h.edit)
	problem.Register(api, huma.Operation{
		OperationID: "get-tournament", Method: http.MethodGet, Path: "/api/v1/tournaments/{tournamentId}",
		Summary: "Get a Tournament", Tags: []string{"Tournaments"},
	}, h.get)
	problem.Register(api, huma.Operation{
		OperationID: "list-tournaments", Method: http.MethodGet, Path: "/api/v1/tournaments",
		Summary: "List Tournaments", Tags: []string{"Tournaments"},
	}, h.list)
	problem.Register(api, huma.Operation{
		OperationID: "cancel-tournament", Method: http.MethodPost, Path: "/api/v1/tournaments/{tournamentId}/cancel",
		Summary: "Cancel a Tournament", Tags: []string{"Tournaments"}, Security: auth.Security(),
		Description: "The Organizer can cancel at any point before completion. Pending Matches stop and their Game Servers are released.",
	}, h.cancel)
}

type handlers struct{ svc *tournament.Service }

type createInput struct {
	Body NewTournament
}

type editInput struct {
	TournamentID ids.TournamentID `path:"tournamentId"`
	Body         TournamentSettings
}

type idInput struct {
	TournamentID ids.TournamentID `path:"tournamentId"`
}

type listInput struct {
	Status string `query:"status" enum:"draft,registration-open,check-in,running,completed,cancelled" doc:"Only Tournaments in this status"`
	GameID string `query:"gameId" doc:"Only Tournaments of this Game"`
	Limit  int32  `query:"limit" minimum:"1" maximum:"200" default:"50"`
	Offset int32  `query:"offset" minimum:"0" default:"0"`
}

type tournamentOutput struct {
	Body TournamentView
}

type listOutput struct {
	Body struct {
		Tournaments []TournamentView `json:"tournaments"`
	}
}

func (h handlers) create(ctx context.Context, in *createInput) (*tournamentOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	v, err := Create(ctx, h.svc, p, in.Body)
	return &tournamentOutput{v}, err
}

func (h handlers) edit(ctx context.Context, in *editInput) (*tournamentOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	v, err := Edit(ctx, h.svc, p, in.TournamentID, in.Body)
	return &tournamentOutput{v}, err
}

func (h handlers) get(ctx context.Context, in *idInput) (*tournamentOutput, error) {
	v, err := Find(ctx, h.svc.Queries, in.TournamentID)
	return &tournamentOutput{v}, err
}

func (h handlers) list(ctx context.Context, in *listInput) (*listOutput, error) {
	vs, err := List(ctx, h.svc.Queries, *in)
	out := &listOutput{}
	out.Body.Tournaments = vs
	return out, err
}

func (h handlers) cancel(ctx context.Context, in *idInput) (*tournamentOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	v, err := Cancel(ctx, h.svc, p, in.TournamentID)
	return &tournamentOutput{v}, err
}

// Create creates a Tournament whose Registration Window opens on schedule.
func Create(ctx context.Context, svc *tournament.Service, p auth.Principal, body NewTournament) (TournamentView, error) {
	game, ok := svc.Games.Get(body.GameID)
	if !ok {
		return TournamentView{}, problem.Fields{{Location: "body.gameId", Message: "no such game", Value: body.GameID}}.Err()
	}
	if p.IsService() && !game.Trusts(p.ClientID) {
		return TournamentView{}, problem.New(problem.Forbidden, "not-trusted-for-game", "client %q is not trusted to act for game %q", p.ClientID, game.ID)
	}
	if err := body.TournamentSettings.validate(game, svc.Clock.Now()); err != nil {
		return TournamentView{}, err
	}
	id := ids.New[ids.TournamentID]()
	row := db.InsertTournamentParams{
		ID: id, GameID: game.ID, Name: body.Name, Organizer: p.ID(), Status: tournament.Draft,
		StartsAt: body.StartsAt.UTC(), RegistrationOpensAt: body.RegistrationOpensAt.UTC(),
		Capacity: body.Capacity, MinimumParticipants: body.MinimumParticipants,
		CheckInEnabled: body.CheckIn.Enabled, CheckInSeconds: body.CheckIn.WindowSeconds, CreatedAt: svc.Clock.Now(),
	}
	if err := svc.Create(ctx, row, body.TournamentSettings.apply); err != nil {
		return TournamentView{}, err
	}
	return Find(ctx, svc.Queries, id)
}

// Edit replaces the settings of a draft Tournament.
func Edit(ctx context.Context, svc *tournament.Service, p auth.Principal, id ids.TournamentID, s TournamentSettings) (TournamentView, error) {
	err := svc.InTournament(ctx, id, func(tx *tournament.Tx) error {
		if err := tx.RequireOrganizer(p); err != nil {
			return err
		}
		if tx.T.Status != tournament.Draft {
			return problem.New(problem.Conflict, "settings-frozen", "settings are frozen once registration opens")
		}
		game, err := tx.Game()
		if err != nil {
			return err
		}
		if err := s.validate(game, tx.Now()); err != nil {
			return err
		}
		if err := tx.Q.UpdateTournamentSettings(tx.Ctx(), db.UpdateTournamentSettingsParams{
			ID: id, Name: s.Name, StartsAt: s.StartsAt.UTC(), RegistrationOpensAt: s.RegistrationOpensAt.UTC(),
			Capacity: s.Capacity, MinimumParticipants: s.MinimumParticipants,
			CheckInEnabled: s.CheckIn.Enabled, CheckInSeconds: s.CheckIn.WindowSeconds, UpdatedAt: tx.Now(),
		}); err != nil {
			return err
		}
		tx.T.RegistrationOpensAt = s.RegistrationOpensAt.UTC()
		return s.apply(tx)
	})
	if err != nil {
		return TournamentView{}, err
	}
	return Find(ctx, svc.Queries, id)
}

// apply replaces the Stages and schedules registration to open.
func (s TournamentSettings) apply(tx *tournament.Tx) error {
	if err := tx.Q.DeleteStages(tx.Ctx(), tx.T.ID); err != nil {
		return err
	}
	for i, st := range s.Stages {
		if err := tx.Q.InsertStage(tx.Ctx(), db.InsertStageParams{
			ID: ids.New[ids.StageID](), TournamentID: tx.T.ID, Position: int32(i), Format: st.Format,
			GroupCount: st.Groups, Advancement: st.Advancement, BestOf: st.BestOf, Bouts: st.Bouts,
			SwissRounds: st.SwissRounds, ResultDeadlineSeconds: st.ResultDeadlineSeconds,
		}); err != nil {
			return err
		}
	}
	return tx.Schedule(tournament.JobOpenRegistration, ids.MatchID{}, tx.T.RegistrationOpensAt)
}

// Cancel stops a Tournament that hasn't completed.
func Cancel(ctx context.Context, svc *tournament.Service, p auth.Principal, id ids.TournamentID) (TournamentView, error) {
	err := svc.InTournament(ctx, id, func(tx *tournament.Tx) error {
		if err := tx.RequireOrganizer(p); err != nil {
			return err
		}
		if tx.T.Status == tournament.Completed || tx.T.Status == tournament.Cancelled {
			return problem.New(problem.Conflict, problem.CodeWrongStatus, "a %s tournament can't be cancelled", tx.T.Status)
		}
		return tx.Cancel()
	})
	if err != nil {
		return TournamentView{}, err
	}
	return Find(ctx, svc.Queries, id)
}

// Find reads one Tournament.
func Find(ctx context.Context, q *db.Queries, id ids.TournamentID) (TournamentView, error) {
	t, err := q.GetTournament(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return TournamentView{}, tournament.ErrTournamentNotFound
	} else if err != nil {
		return TournamentView{}, err
	}
	vs, err := views(ctx, q, []db.Tournament{t})
	if err != nil {
		return TournamentView{}, err
	}
	return vs[0], nil
}

// List reads Tournaments, latest start first.
func List(ctx context.Context, q *db.Queries, in listInput) ([]TournamentView, error) {
	params := db.ListTournamentsParams{MaxResults: in.Limit, Skip: in.Offset}
	if in.Status != "" {
		params.Status = &in.Status
	}
	if in.GameID != "" {
		params.GameID = &in.GameID
	}
	ts, err := q.ListTournaments(ctx, params)
	if err != nil {
		return nil, err
	}
	return views(ctx, q, ts)
}

func views(ctx context.Context, q *db.Queries, ts []db.Tournament) ([]TournamentView, error) {
	tids := make([]ids.TournamentID, len(ts))
	for i, t := range ts {
		tids[i] = t.ID
	}
	counts, err := q.RegistrationCounts(ctx, ids.UUIDs(tids))
	if err != nil {
		return nil, err
	}
	stages, err := q.ListStagesOfTournaments(ctx, ids.UUIDs(tids))
	if err != nil {
		return nil, err
	}
	out := make([]TournamentView, len(ts))
	for i, t := range ts {
		v := TournamentView{
			ID: t.ID, GameID: t.GameID, Name: t.Name, Organizer: t.Organizer, Status: t.Status,
			StartsAt: t.StartsAt, RegistrationOpensAt: t.RegistrationOpensAt,
			Capacity: t.Capacity, MinimumParticipants: t.MinimumParticipants,
			CheckIn:   CheckInSettings{Enabled: t.CheckInEnabled, WindowSeconds: t.CheckInSeconds},
			CreatedAt: t.CreatedAt, UpdatedAt: t.UpdatedAt, CompletedAt: t.CompletedAt,
			Stages: []ConfiguredStageView{},
		}
		if t.CheckInEnabled {
			opens := tournament.CheckInOpensAt(t)
			v.CheckInOpensAt = &opens
		}
		for _, c := range counts {
			if c.TournamentID == t.ID {
				v.Registered, v.CheckedIn = c.Registered, c.CheckedIn
			}
		}
		for _, s := range stages {
			if s.TournamentID == t.ID {
				v.Stages = append(v.Stages, ConfiguredStageView{
					ID: s.ID, Position: s.Position, Status: s.Status,
					StageSettings: StageSettings{
						Format: s.Format, Groups: s.GroupCount, Advancement: s.Advancement, BestOf: s.BestOf,
						Bouts: s.Bouts, SwissRounds: s.SwissRounds, ResultDeadlineSeconds: s.ResultDeadlineSeconds,
					},
				})
			}
		}
		out[i] = v
	}
	return out, nil
}

// TournamentView is a Tournament as the API shows it.
type TournamentView struct {
	ID                  ids.TournamentID      `json:"id"`
	GameID              string                `json:"gameId"`
	Name                string                `json:"name"`
	Organizer           string                `json:"organizer" doc:"The Organizer: user:<keycloak subject> or client:<keycloak client id>"`
	Status              string                `json:"status" enum:"draft,registration-open,check-in,running,completed,cancelled"`
	StartsAt            time.Time             `json:"startsAt"`
	RegistrationOpensAt time.Time             `json:"registrationOpensAt"`
	CheckInOpensAt      *time.Time            `json:"checkInOpensAt,omitempty"`
	Capacity            int32                 `json:"capacity"`
	MinimumParticipants int32                 `json:"minimumParticipants"`
	CheckIn             CheckInSettings       `json:"checkIn"`
	Stages              []ConfiguredStageView `json:"stages"`
	Registered          int32                 `json:"registered" doc:"Participants registered, checked in or playing"`
	CheckedIn           int32                 `json:"checkedIn"`
	CreatedAt           time.Time             `json:"createdAt"`
	UpdatedAt           time.Time             `json:"updatedAt"`
	CompletedAt         *time.Time            `json:"completedAt,omitempty"`
}

// ConfiguredStageView is a configured Stage.
type ConfiguredStageView struct {
	ID       ids.StageID `json:"id"`
	Position int32       `json:"position"`
	Status   string      `json:"status" enum:"pending,running,completed"`
	StageSettings
}
