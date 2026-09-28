// Package matches is the slice for reading a Match, including its Game
// Server address for its Participants and Organizer, and for resolving a
// Stalled Match.
package matches

import (
	"context"
	"errors"
	"net/http"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/features/structure"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/jackc/pgx/v5"
)

var keycloak = []map[string][]string{{"keycloak": {}}}

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc}
	problem.Register(api, huma.Operation{
		OperationID: "get-match", Method: http.MethodGet, Path: "/api/v1/matches/{matchId}",
		Summary: "Get a Match", Tags: []string{"Matches"},
		Description: "The Game Server address and port are included only for the Match's Participants and the Organizer.",
	}, h.get)
	problem.Register(api, huma.Operation{
		OperationID: "resolve-stalled-match", Method: http.MethodPost, Path: "/api/v1/matches/{matchId}/resolve",
		Summary: "Resolve a Stalled Match", Tags: []string{"Matches"}, Security: keycloak,
		Description: "The Organizer awards a win, or a double Forfeit. This is the only manual way to set a result.",
	}, h.resolve)
}

type handlers struct{ svc *tournament.Service }

type matchInput struct {
	MatchID ids.MatchID `path:"matchId"`
}

type resolveInput struct {
	MatchID ids.MatchID `path:"matchId"`
	Body    struct {
		Winner        *ids.ParticipantID `json:"winner,omitempty" doc:"The Participant awarded the win"`
		DoubleForfeit bool               `json:"doubleForfeit,omitempty" doc:"Every Participant forfeits"`
	}
}

type matchOutput struct{ Body MatchDetailView }

func (h handlers) get(ctx context.Context, in *matchInput) (*matchOutput, error) {
	p, err := auth.Optional(ctx)
	if err != nil {
		return nil, err
	}
	v, err := Get(ctx, h.svc.Queries, p, in.MatchID)
	return &matchOutput{v}, err
}

func (h handlers) resolve(ctx context.Context, in *resolveInput) (*matchOutput, error) {
	p, err := auth.Required(ctx)
	if err != nil {
		return nil, err
	}
	r := tournament.Resolution{DoubleForfeit: in.Body.DoubleForfeit}
	if in.Body.Winner != nil {
		r.Winner = *in.Body.Winner
	}
	if err := Resolve(ctx, h.svc, p, in.MatchID, r); err != nil {
		return nil, err
	}
	v, err := Get(ctx, h.svc.Queries, &p, in.MatchID)
	return &matchOutput{v}, err
}

// Resolve settles a Stalled Match for its Organizer.
func Resolve(ctx context.Context, svc *tournament.Service, p auth.Principal, id ids.MatchID, r tournament.Resolution) error {
	if r.DoubleForfeit == !r.Winner.IsZero() {
		return problem.Fields{{Location: "body", Message: "give either a winner or doubleForfeit: true"}}.Err()
	}
	return svc.InMatch(ctx, id, func(tx *tournament.Tx, m db.Match) error {
		if err := tx.RequireOrganizer(p); err != nil {
			return err
		}
		return tx.ResolveStalled(m, r)
	})
}

// MatchDetailView is a Match, with its Game Server for those allowed to see it.
type MatchDetailView struct {
	structure.MatchView
	ServerAddress *string `json:"serverAddress,omitempty" doc:"Only for the Match's Participants and the Organizer"`
	ServerPort    *int32  `json:"serverPort,omitempty" doc:"Only for the Match's Participants and the Organizer"`
}

// Get reads a Match.
func Get(ctx context.Context, q *db.Queries, p *auth.Principal, id ids.MatchID) (MatchDetailView, error) {
	m, err := q.GetMatch(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return MatchDetailView{}, tournament.ErrMatchNotFound
	} else if err != nil {
		return MatchDetailView{}, err
	}
	slotRows, err := q.ListSlotsOfMatches(ctx, ids.UUIDs([]ids.MatchID{id}))
	if err != nil {
		return MatchDetailView{}, err
	}
	bouts, err := q.ListBoutsOfMatch(ctx, id)
	if err != nil {
		return MatchDetailView{}, err
	}
	var slots []ids.ParticipantID
	for _, s := range slotRows {
		slots = append(slots, s.ParticipantID)
	}
	v := MatchDetailView{MatchView: structure.MatchViewOf(m, slots, bouts)}
	if !v.ServerAllocated || p == nil {
		return v, nil
	}
	allowed, err := mayConnect(ctx, q, *p, m, slots)
	if err != nil {
		return MatchDetailView{}, err
	}
	if allowed {
		v.ServerAddress, v.ServerPort = m.ServerAddress, m.ServerPort
	}
	return v, nil
}

// mayConnect reports whether the caller is one of the Match's Participants
// or the Organizer.
func mayConnect(ctx context.Context, q *db.Queries, p auth.Principal, m db.Match, slots []ids.ParticipantID) (bool, error) {
	t, err := q.GetTournament(ctx, m.TournamentID)
	if err != nil {
		return false, err
	}
	if t.Organizer == p.ID() {
		return true, nil
	}
	people, err := q.ListParticipantsByIds(ctx, ids.UUIDs(slots))
	if err != nil {
		return false, err
	}
	for _, person := range people {
		if p.Owns(auth.Identity{Kind: person.IdentityKind, Value: person.IdentityValue}) {
			return true, nil
		}
	}
	return false, nil
}
