package tournament

import (
	"context"
	"fmt"
	"math/rand/v2"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/ids"
)

// CheckInOpensAt is when the Check-in Window of a Tournament opens.
func CheckInOpensAt(t db.Tournament) time.Time {
	return t.StartsAt.Add(-time.Duration(t.CheckInSeconds) * time.Second)
}

// OpenRegistration opens the Registration Window, and the Check-in Window
// too if it is already due.
func (s *Service) OpenRegistration(ctx context.Context, id ids.TournamentID) error {
	return s.InTournament(ctx, id, func(tx *Tx) error {
		if tx.T.Status != Draft {
			return nil
		}
		if err := tx.SetStatus(RegistrationOpen); err != nil {
			return err
		}
		if err := tx.Schedule(JobStart, ids.MatchID{}, tx.T.StartsAt); err != nil {
			return err
		}
		if !tx.T.CheckInEnabled {
			return nil
		}
		if opens := CheckInOpensAt(tx.T); opens.After(tx.now) {
			return tx.Schedule(JobOpenCheckIn, ids.MatchID{}, opens)
		}
		return tx.SetStatus(CheckIn)
	})
}

// OpenCheckIn opens the Check-in Window.
func (s *Service) OpenCheckIn(ctx context.Context, id ids.TournamentID) error {
	return s.InTournament(ctx, id, func(tx *Tx) error {
		if tx.T.Status != RegistrationOpen {
			return nil
		}
		return tx.SetStatus(CheckIn)
	})
}

// Start starts a Tournament at its start time. Participants who didn't check
// in are dropped, and a Tournament below Minimum Participants is cancelled.
func (s *Service) Start(ctx context.Context, id ids.TournamentID) error {
	return s.InTournament(ctx, id, func(tx *Tx) error {
		if tx.T.Status != RegistrationOpen && tx.T.Status != CheckIn {
			return nil
		}
		if tx.T.CheckInEnabled {
			if err := tx.Q.DropNotCheckedIn(tx.ctx, tx.T.ID); err != nil {
				return err
			}
		}
		count, err := tx.Q.CountRegistered(tx.ctx, tx.T.ID)
		if err != nil {
			return err
		}
		if count < tx.T.MinimumParticipants {
			return tx.cancel()
		}
		active, err := tx.Q.ActivateParticipants(tx.ctx, tx.T.ID)
		if err != nil {
			return err
		}
		if err := tx.SetStatus(Running); err != nil {
			return err
		}
		stages, err := tx.Q.ListStages(tx.ctx, tx.T.ID)
		if err != nil {
			return err
		}
		seeds := make([]format.ParticipantID, len(active))
		for i, p := range active {
			seeds[i] = pid(p)
		}
		tx.s.random(func(r *rand.Rand) { seeds = format.RandomSeeding(seeds, r) })
		return tx.startStage(stages[0], seeds)
	})
}

// startStage splits the seeded Participants into Groups by snake Seeding and
// opens every Group.
func (tx *Tx) startStage(st db.Stage, seeds []format.ParticipantID) error {
	if err := tx.Q.SetStageStatus(tx.ctx, db.SetStageStatusParams{ID: st.ID, Status: StageRunning}); err != nil {
		return err
	}
	if err := tx.Emit(events.StageStarted, map[string]any{"stageId": st.ID, "position": st.Position}); err != nil {
		return err
	}
	var groups []ids.GroupID
	for i, members := range format.Snake(seeds, int(st.GroupCount)) {
		g := ids.New[ids.GroupID]()
		groups = append(groups, g)
		if err := tx.Q.InsertGroup(tx.ctx, db.InsertGroupParams{ID: g, TournamentID: tx.T.ID, StageID: st.ID, Position: int32(i)}); err != nil {
			return fmt.Errorf("insert group: %w", err)
		}
		var entrants []format.Entrant
		tx.s.random(func(r *rand.Rand) { entrants = format.DrawLots(members, r) })
		for seed, e := range entrants {
			if err := tx.Q.InsertEntrant(tx.ctx, db.InsertEntrantParams{
				GroupID: g, ParticipantID: ParticipantOf(e.ID), Seed: int32(seed + 1), Lot: int32(e.Lot),
			}); err != nil {
				return fmt.Errorf("insert entrant: %w", err)
			}
		}
	}
	for _, g := range groups {
		if err := tx.ProgressGroup(g); err != nil {
			return err
		}
	}
	return nil
}

// finishStageIfComplete moves on once every Group of a Stage is complete:
// the top Participants of each Group advance into the next Stage, or the
// Tournament completes after the last Stage.
func (tx *Tx) finishStageIfComplete(st db.Stage) error {
	groups, err := tx.Q.ListGroupsOfStage(tx.ctx, st.ID)
	if err != nil {
		return err
	}
	if slices.ContainsFunc(groups, func(g db.Group) bool { return g.Status != StageCompleted }) {
		return nil
	}
	if err := tx.Q.SetStageStatus(tx.ctx, db.SetStageStatusParams{ID: st.ID, Status: StageCompleted}); err != nil {
		return err
	}
	if err := tx.Emit(events.StageCompleted, map[string]any{"stageId": st.ID, "position": st.Position}); err != nil {
		return err
	}
	stages, err := tx.Q.ListStages(tx.ctx, tx.T.ID)
	if err != nil {
		return err
	}
	if int(st.Position) == len(stages)-1 {
		return tx.complete(stages)
	}

	var standings [][]format.Standing
	for _, g := range groups {
		gs, err := tx.loadGroup(g.ID)
		if err != nil {
			return err
		}
		plan, err := gs.plan()
		if err != nil {
			return err
		}
		standings = append(standings, plan.Standings)
	}
	seeds := format.AdvancementSeeding(standings, int(st.Advancement))
	for gi, g := range groups {
		for _, s := range standings[gi] {
			p := ParticipantOf(s.Participant)
			if slices.Contains(seeds, s.Participant) {
				if err := tx.Q.MarkAdvanced(tx.ctx, db.MarkAdvancedParams{GroupID: g.ID, ParticipantID: p}); err != nil {
					return err
				}
				continue
			}
			if person, err := tx.Participant(p); err != nil {
				return err
			} else if person.Status == Active {
				if err := tx.setParticipantStatus(p, Eliminated); err != nil {
					return err
				}
			}
		}
	}
	return tx.startStage(stages[st.Position+1], seeds)
}

// complete ends the Tournament with Final Placements for everyone who played.
func (tx *Tx) complete(stages []db.Stage) error {
	var results [][]format.GroupResult
	for _, st := range stages {
		groups, err := tx.Q.ListGroupsOfStage(tx.ctx, st.ID)
		if err != nil {
			return err
		}
		var stage []format.GroupResult
		for _, g := range groups {
			gs, err := tx.loadGroup(g.ID)
			if err != nil {
				return err
			}
			plan, err := gs.plan()
			if err != nil {
				return err
			}
			r := format.GroupResult{Placements: plan.Placements}
			for _, e := range gs.entrants {
				if e.Advanced {
					r.Advanced = append(r.Advanced, pid(e.ParticipantID))
				}
			}
			stage = append(stage, r)
		}
		results = append(results, stage)
	}
	placements := format.FinalPlacements(results)
	for _, pl := range placements {
		if err := tx.Q.SetFinalPlacement(tx.ctx, db.SetFinalPlacementParams{
			ID: ParticipantOf(pl.Participant), FinalFrom: ptr(int32(pl.From)), FinalTo: ptr(int32(pl.To)),
		}); err != nil {
			return err
		}
	}
	if err := tx.Q.CompleteTournament(tx.ctx, db.CompleteTournamentParams{ID: tx.T.ID, UpdatedAt: tx.now}); err != nil {
		return err
	}
	tx.T.Status = Completed
	view := make([]PlacementView, len(placements))
	for i, pl := range placements {
		view[i] = PlacementView{ParticipantID: ParticipantOf(pl.Participant), From: pl.From, To: pl.To}
	}
	return tx.Emit(events.TournamentCompleted, map[string]any{"status": Completed, "placements": view})
}

// Cancel stops a Tournament before it completes: pending Matches stop and
// their Game Servers are released.
func (tx *Tx) Cancel() error { return tx.cancel() }

func (tx *Tx) cancel() error {
	cancelled, err := tx.Q.CancelOpenMatches(tx.ctx, tx.T.ID)
	if err != nil {
		return err
	}
	for _, m := range cancelled {
		tx.release(m)
	}
	if err := tx.Q.UnscheduleTournamentJobs(tx.ctx, tx.T.ID); err != nil {
		return err
	}
	return tx.SetStatus(Cancelled)
}

func ptr[T any](v T) *T { return &v }
