// Package structure is the slice for reading a Tournament's full structure,
// Standings and Final Placements. Standings are computed by the Format
// engine from persisted results.
package structure

import (
	"context"
	"errors"
	"net/http"
	"slices"
	"strconv"
	"strings"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/jackc/pgx/v5"
)

// Register registers the slice's operations.
func Register(api huma.API, svc *tournament.Service) {
	h := handlers{svc.Queries}
	problem.Register(api, huma.Operation{
		OperationID: "get-structure", Method: http.MethodGet, Path: "/api/v1/tournaments/{tournamentId}/structure",
		Summary: "Get a Tournament's Stages, Groups, Rounds, Matches, Bouts and Standings", Tags: []string{"Structure"},
		Description: "Everything a frontend needs to draw brackets and tables. Game Server addresses are never included; " +
			"a Match's Participants get theirs from the Match itself.",
	}, h.structure)
	problem.Register(api, huma.Operation{
		OperationID: "get-placements", Method: http.MethodGet, Path: "/api/v1/tournaments/{tournamentId}/placements",
		Summary: "Get Final Placements", Tags: []string{"Structure"},
		Description: "Empty until the Tournament completes. Participants knocked out in the same elimination Round share a range.",
	}, h.placements)
}

type handlers struct{ q *db.Queries }

type input struct {
	TournamentID ids.TournamentID `path:"tournamentId"`
}

type structureOutput struct{ Body StructureView }

type placementsOutput struct{ Body PlacementsView }

func (h handlers) structure(ctx context.Context, in *input) (*structureOutput, error) {
	v, err := Find(ctx, h.q, in.TournamentID)
	return &structureOutput{v}, err
}

func (h handlers) placements(ctx context.Context, in *input) (*placementsOutput, error) {
	v, err := FinalPlacements(ctx, h.q, in.TournamentID)
	return &placementsOutput{v}, err
}

// StructureView is a Tournament's structure.
type StructureView struct {
	TournamentID ids.TournamentID           `json:"tournamentId"`
	Status       lifecycle.TournamentStatus `json:"status"`
	Stages       []StageView                `json:"stages"`
}

// StageView is a Stage with its Groups.
type StageView struct {
	ID       ids.StageID `json:"id"`
	Position int32       `json:"position"`
	Format   string      `json:"format"`
	Status   string      `json:"status"`
	Groups   []GroupView `json:"groups"`
}

// GroupView is a Group with its Standings and Rounds.
type GroupView struct {
	ID           ids.GroupID               `json:"id"`
	Position     int32                     `json:"position"`
	Status       string                    `json:"status" enum:"running,completed"`
	Participants []GroupParticipantView    `json:"participants"`
	Standings    []tournament.StandingView `json:"standings"`
	Rounds       []RoundView               `json:"rounds"`
}

// GroupParticipantView is a Participant's Seeding in a Group.
type GroupParticipantView struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	Seed          int32             `json:"seed"`
	Advanced      bool              `json:"advanced,omitempty"`
}

// RoundView is one Round of a Group (per bracket, in double elimination).
type RoundView struct {
	Round   int32       `json:"round"`
	Bracket string      `json:"bracket,omitempty" enum:"upper,lower,grand-final"`
	Matches []MatchView `json:"matches"`
}

// MatchView is a Match without its Game Server address.
type MatchView struct {
	ID              ids.MatchID           `json:"id"`
	TournamentID    ids.TournamentID      `json:"tournamentId"`
	GroupID         ids.GroupID           `json:"groupId"`
	Key             string                `json:"key" doc:"Stable position of the Match in its Group, e.g. R2-M1"`
	Round           int32                 `json:"round"`
	Bracket         string                `json:"bracket,omitempty"`
	Status          string                `json:"status" enum:"pending,ready,allocating,in-progress,stalled,completed,cancelled"`
	Result          *string               `json:"result,omitempty" enum:"win,double-forfeit,bye,empty,free-for-all"`
	WinnerID        *ids.ParticipantID    `json:"winnerId,omitempty"`
	Participants    []ids.ParticipantID   `json:"participants"`
	Bouts           []tournament.BoutView `json:"bouts"`
	ReadyAt         *time.Time            `json:"readyAt,omitempty"`
	ResultDeadline  *time.Time            `json:"resultDeadline,omitempty"`
	StartedAt       *time.Time            `json:"startedAt,omitempty"`
	CompletedAt     *time.Time            `json:"completedAt,omitempty"`
	Aborts          int32                 `json:"aborts" doc:"How often its Game Server failed mid-play"`
	ServerAllocated bool                  `json:"serverAllocated"`
}

// MatchViewOf builds a MatchView.
func MatchViewOf(m db.Match, slots []ids.ParticipantID, bouts []db.BoutResult) MatchView {
	v := MatchView{
		ID: m.ID, TournamentID: m.TournamentID, GroupID: m.GroupID, Key: m.Key, Round: m.Round, Bracket: m.Bracket,
		Status: m.Status, Result: m.Result, Participants: slots, Bouts: tournament.BoutsView(bouts),
		ReadyAt: m.ReadyAt, ResultDeadline: m.ResultDeadline, StartedAt: m.StartedAt, CompletedAt: m.CompletedAt,
		Aborts: m.Aborts, ServerAllocated: m.ServerName != nil && (m.Status == tournament.MatchAllocating || m.Status == tournament.MatchInProgress),
	}
	if v.Participants == nil {
		v.Participants = []ids.ParticipantID{}
	}
	if !m.WinnerID.IsZero() {
		w := m.WinnerID
		v.WinnerID = &w
	}
	return v
}

// Find reads a Tournament's structure.
func Find(ctx context.Context, q *db.Queries, tid ids.TournamentID) (StructureView, error) {
	t, err := q.GetTournament(ctx, tid)
	if errors.Is(err, pgx.ErrNoRows) {
		return StructureView{}, tournament.ErrTournamentNotFound
	} else if err != nil {
		return StructureView{}, err
	}
	stages, err := q.ListStages(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	groups, err := q.ListGroups(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	participants, err := q.ListGroupParticipantsOfTournament(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	matches, err := q.ListMatchesOfTournament(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	slotRows, err := q.ListSlotsOfTournament(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	boutRows, err := q.ListBoutsOfTournament(ctx, tid)
	if err != nil {
		return StructureView{}, err
	}
	slots := map[ids.MatchID][]ids.ParticipantID{}
	for _, s := range slotRows {
		slots[s.MatchID] = append(slots[s.MatchID], s.ParticipantID)
	}
	bouts := map[ids.MatchID][]db.BoutResult{}
	for _, b := range boutRows {
		bouts[b.MatchID] = append(bouts[b.MatchID], b)
	}

	v := StructureView{TournamentID: tid, Status: t.Status, Stages: []StageView{}}
	for _, st := range stages {
		sv := StageView{ID: st.ID, Position: st.Position, Format: st.Format, Status: st.Status, Groups: []GroupView{}}
		for _, g := range groups {
			if g.StageID != st.ID {
				continue
			}
			gv := GroupView{ID: g.ID, Position: g.Position, Status: g.Status, Participants: []GroupParticipantView{}, Rounds: []RoundView{}}
			eg := format.Group{Stage: tournament.StageRules(st)}
			for _, e := range participants {
				if e.GroupID != g.ID {
					continue
				}
				gv.Participants = append(gv.Participants, GroupParticipantView{ParticipantID: e.ParticipantID, Seed: e.Seed, Advanced: e.Advanced})
				fid := format.ParticipantID(e.ParticipantID.String())
				eg.Participants = append(eg.Participants, format.Participant{ID: fid, Lot: int(e.Lot)})
				if e.Status == tournament.Withdrawn || e.Status == tournament.Disqualified {
					eg.Dropped = append(eg.Dropped, fid)
				}
			}
			for _, m := range matches {
				if m.GroupID != g.ID {
					continue
				}
				fm := format.Match{Key: m.Key, Round: int(m.Round), Bouts: tournament.EngineBouts(bouts[m.ID])}
				for _, p := range slots[m.ID] {
					fm.Participants = append(fm.Participants, format.ParticipantID(p.String()))
				}
				eg.Matches = append(eg.Matches, fm)
				i := slices.IndexFunc(gv.Rounds, func(r RoundView) bool { return r.Round == m.Round && r.Bracket == m.Bracket })
				if i < 0 {
					gv.Rounds = append(gv.Rounds, RoundView{Round: m.Round, Bracket: m.Bracket})
					i = len(gv.Rounds) - 1
				}
				gv.Rounds[i].Matches = append(gv.Rounds[i].Matches, MatchViewOf(m, slots[m.ID], bouts[m.ID]))
			}
			for _, r := range gv.Rounds {
				slices.SortStableFunc(r.Matches, func(a, b MatchView) int { return matchNumber(a.Key) - matchNumber(b.Key) })
			}
			slices.SortStableFunc(gv.Rounds, func(a, b RoundView) int {
				if a.Bracket != b.Bracket {
					return bracketOrder(a.Bracket) - bracketOrder(b.Bracket)
				}
				return int(a.Round - b.Round)
			})
			plan, err := format.PlanGroup(eg)
			if err != nil {
				return StructureView{}, err
			}
			gv.Standings = tournament.StandingsView(plan.Standings)
			sv.Groups = append(sv.Groups, gv)
		}
		v.Stages = append(v.Stages, sv)
	}
	return v, nil
}

// matchNumber is the position of a Match in its Round: 10 in R1-M10.
func matchNumber(key string) int {
	_, n, ok := strings.Cut(key, "-M")
	if !ok {
		return 0
	}
	i, _ := strconv.Atoi(n)
	return i
}

func bracketOrder(b string) int {
	return slices.Index([]string{"", "upper", "lower", "grand-final"}, b)
}

// PlacementsView are a Tournament's Final Placements.
type PlacementsView struct {
	TournamentID ids.TournamentID           `json:"tournamentId"`
	Status       lifecycle.TournamentStatus `json:"status"`
	Placements   []PlacementView            `json:"placements"`
}

// PlacementView is one Participant's Final Placement.
type PlacementView struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	Identity      auth.Identity     `json:"identity"`
	From          int32             `json:"from" doc:"Best place of the shared range"`
	To            int32             `json:"to" doc:"Worst place of the shared range"`
}

// FinalPlacements reads a Tournament's Final Placements.
func FinalPlacements(ctx context.Context, q *db.Queries, tid ids.TournamentID) (PlacementsView, error) {
	t, err := q.GetTournament(ctx, tid)
	if errors.Is(err, pgx.ErrNoRows) {
		return PlacementsView{}, tournament.ErrTournamentNotFound
	} else if err != nil {
		return PlacementsView{}, err
	}
	ps, err := q.ListFinalPlacements(ctx, tid)
	if err != nil {
		return PlacementsView{}, err
	}
	out := PlacementsView{TournamentID: tid, Status: t.Status, Placements: []PlacementView{}}
	for _, p := range ps {
		out.Placements = append(out.Placements, PlacementView{
			ParticipantID: p.ID, Identity: auth.Identity{Kind: p.IdentityKind, Value: p.IdentityValue},
			From: *p.FinalFrom, To: *p.FinalTo,
		})
	}
	return out, nil
}
