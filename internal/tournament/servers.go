package tournament

import (
	"context"
	"errors"
	"fmt"
	"log/slog"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/matchtoken"
	"github.com/jackc/pgx/v5"
)

// Allocate gets a Game Server for a Ready Match. The allocation attempt is
// recorded before asking Agones, so a server allocated just before a crash
// is recognized as leaked. A failure is returned so the job is retried with
// backoff.
func (s *Service) Allocate(ctx context.Context, id ids.MatchID) error {
	var req gameserver.AllocationRequest
	err := s.InMatch(ctx, id, func(tx *Tx, m db.Match) error {
		if (m.Status != MatchReady && m.Status != MatchAllocating) || m.ServerName != nil {
			return nil
		}
		game, ok := s.Games.Get(tx.T.GameID)
		if !ok {
			return fmt.Errorf("game %q is no longer configured", tx.T.GameID)
		}
		allocation := ids.New[ids.AllocationID]()
		wasReady := m.Status == MatchReady
		m, err := tx.Q.RequestAllocation(tx.ctx, db.RequestAllocationParams{ID: m.ID, AllocationID: allocation})
		if err != nil {
			return err
		}
		token, err := s.Tokens.Issue(matchtoken.Claims{MatchID: m.ID, AllocationID: allocation}, *m.ResultDeadline)
		if err != nil {
			return err
		}
		req = gameserver.AllocationRequest{Fleet: game.Fleet, MatchID: m.ID, AllocationID: allocation, Token: token}
		if !wasReady {
			return nil // a retry: nothing visible changed
		}
		gs, err := tx.loadGroup(m.GroupID)
		if err != nil {
			return err
		}
		return tx.EmitMatch(m, gs.slots[m.ID])
	})
	if err != nil || req.Token == "" {
		return err
	}

	server, err := s.Servers.Allocate(ctx, req)
	if err != nil {
		return fmt.Errorf("allocate game server for match %s: %w", id, err)
	}

	confirmed := false
	err = s.InMatch(ctx, id, func(tx *Tx, m db.Match) error {
		m, err := tx.Q.ConfirmAllocation(tx.ctx, db.ConfirmAllocationParams{
			ID: m.ID, AllocationID: req.AllocationID, ServerName: &server.Name,
			ServerAddress: &server.Address, ServerPort: ptr(int32(server.Port)),
		})
		if errors.Is(err, pgx.ErrNoRows) {
			return nil // the Match moved on meanwhile
		} else if err != nil {
			return err
		}
		confirmed = true
		gs, err := tx.loadGroup(m.GroupID)
		if err != nil {
			return err
		}
		if err := tx.EmitMatch(m, gs.slots[m.ID]); err != nil {
			return err
		}
		// Participants who left while the server was being allocated.
		return tx.notifyForfeitsIfAny(gs, m)
	})
	if err != nil || !confirmed {
		if releaseErr := s.Servers.Release(ctx, server.Name); releaseErr != nil {
			slog.WarnContext(ctx, "release unused game server", "server", server.Name, "error", releaseErr)
		}
	}
	return err
}

func (tx *Tx) notifyForfeitsIfAny(gs *groupState, m db.Match) error {
	for _, p := range gs.slots[m.ID] {
		if gs.isDropped(p) {
			return tx.notifyForfeits(m.ID)
		}
	}
	return nil
}

// Reconcile compares every Match on a Game Server with the servers that
// actually exist, level-triggered:
//
//   - a Match whose server failed or vanished is Aborted and reallocated;
//   - a server whose Match is over, or that no Match references, is released.
//
// Every replica may run it: the conditional updates let exactly one act.
func (s *Service) Reconcile(ctx context.Context) error {
	servers, err := s.Servers.Servers(ctx)
	if err != nil {
		return fmt.Errorf("list game servers: %w", err)
	}
	byName := make(map[string]gameserver.Server, len(servers))
	matchIDs := make([]ids.MatchID, 0, len(servers))
	for _, sv := range servers {
		byName[sv.Name] = sv
		matchIDs = append(matchIDs, sv.MatchID)
	}

	running, err := s.Queries.ListMatchesOnServers(ctx)
	if err != nil {
		return err
	}
	var errs []error
	for _, m := range running {
		sv, ok := byName[*m.ServerName]
		if ok && sv.State == gameserver.Healthy && sv.AllocationID == m.AllocationID {
			continue
		}
		if err := s.abort(ctx, m); err != nil {
			errs = append(errs, err)
		}
	}

	matches, err := s.Queries.ListMatchesByIds(ctx, ids.UUIDs(matchIDs))
	if err != nil {
		return err
	}
	byID := make(map[ids.MatchID]db.Match, len(matches))
	for _, m := range matches {
		byID[m.ID] = m
	}
	for _, sv := range servers {
		m, ok := byID[sv.MatchID]
		current := ok && m.AllocationID == sv.AllocationID && (m.Status == MatchAllocating || m.Status == MatchInProgress)
		if current && sv.State == gameserver.Healthy {
			continue // still in use, or its allocation is being confirmed
		}
		if err := s.Servers.Release(ctx, sv.Name); err != nil {
			errs = append(errs, fmt.Errorf("release %s: %w", sv.Name, err))
		}
	}
	return errors.Join(errs...)
}

// abort sends a Match whose Game Server failed back to allocation. Bouts
// already completed are kept, and the Result Deadline keeps running.
func (s *Service) abort(ctx context.Context, failed db.Match) error {
	return s.InMatch(ctx, failed.ID, func(tx *Tx, m db.Match) error {
		m, err := tx.Q.AbortMatch(tx.ctx, db.AbortMatchParams{ID: m.ID, AllocationID: failed.AllocationID})
		if errors.Is(err, pgx.ErrNoRows) {
			return nil // another replica got there first, or the Match moved on
		} else if err != nil {
			return err
		}
		slog.InfoContext(ctx, "match aborted: game server failed", "match", m.ID, "server", *failed.ServerName)
		tx.release(failed)
		if err := tx.Schedule(JobAllocate, m.ID, tx.now); err != nil {
			return err
		}
		gs, err := tx.loadGroup(m.GroupID)
		if err != nil {
			return err
		}
		return tx.EmitMatch(m, gs.slots[m.ID])
	})
}
