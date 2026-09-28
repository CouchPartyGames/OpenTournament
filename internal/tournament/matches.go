package tournament

import (
	"context"
	"errors"
	"log/slog"
	"slices"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/jackc/pgx/v5"
)

// Error codes of the Match lifecycle.
const (
	CodeBoutAlreadyForfeited = "bout-already-forfeited"
	CodeBoutConflict         = "bout-conflict"
	CodeBoutOutOfOrder       = "bout-out-of-order"
	CodeMatchNotStalled      = "match-not-stalled"
	CodeParticipantNotActive = "participant-not-active"
	CodeMatchTokenInvalid    = "match-token-invalid"
)

// ErrMatchNotFound is returned for an unknown Match.
var ErrMatchNotFound = problem.New(problem.NotFound, problem.CodeMatchNotFound, "no such match")

// InMatch runs fn in a transaction on the Tournament a Match belongs to,
// with the Match's Group loaded.
func (s *Service) InMatch(ctx context.Context, id ids.MatchID, fn func(tx *Tx, m db.Match) error) error {
	m, err := s.Queries.GetMatch(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return ErrMatchNotFound
	} else if err != nil {
		return err
	}
	return s.InTournament(ctx, m.TournamentID, func(tx *Tx) error {
		m, err := tx.Q.GetMatch(tx.ctx, id)
		if err != nil {
			return err
		}
		return fn(tx, m)
	})
}

func (gs *groupState) headToHead() bool { return gs.stage.Format.HeadToHead() }

func (gs *groupState) decided(m db.Match) bool {
	parts := make([]format.ParticipantID, 0, 2)
	for _, p := range gs.slots[m.ID] {
		parts = append(parts, pid(p))
	}
	bouts := EngineBouts(gs.bouts[m.ID])
	if gs.headToHead() {
		_, ok := format.HeadToHeadOutcome(int(gs.stage.BestOf), parts, bouts)
		return ok
	}
	dropped := map[format.ParticipantID]bool{}
	for _, e := range gs.participants {
		if e.Status.HasLeft() {
			dropped[pid(e.ParticipantID)] = true
		}
	}
	return format.FreeForAllDecided(int(gs.stage.Bouts), parts, bouts, dropped)
}

// boutResults returns one Bout's recorded results by Participant.
func (gs *groupState) boutResults(m db.Match, bout int32) map[ids.ParticipantID]db.BoutResult {
	out := map[ids.ParticipantID]db.BoutResult{}
	for _, r := range gs.bouts[m.ID] {
		if r.Bout == bout {
			out[r.ParticipantID] = r
		}
	}
	return out
}

// playedBouts is the number of Bouts with a result for every Participant.
func (gs *groupState) playedBouts(m db.Match) int32 {
	var n int32
	for b := int32(1); ; b++ {
		rs := gs.boutResults(m, b)
		if len(rs) == 0 || !gs.boutComplete(m, rs) {
			return n
		}
		n = b
	}
}

func (gs *groupState) boutComplete(m db.Match, rs map[ids.ParticipantID]db.BoutResult) bool {
	for _, p := range gs.slots[m.ID] {
		if _, ok := rs[p]; !ok && !gs.isDropped(p) {
			return false
		}
	}
	return true
}

func (gs *groupState) isDropped(p ids.ParticipantID) bool {
	for _, e := range gs.participants {
		if e.ParticipantID == p {
			return e.Status.HasLeft()
		}
	}
	return false
}

func (tx *Tx) insertResult(gs *groupState, r db.BoutResult) error {
	r.RecordedAt = tx.now
	if err := tx.Q.InsertBoutResult(tx.ctx, db.InsertBoutResultParams{
		MatchID: r.MatchID, Bout: r.Bout, ParticipantID: r.ParticipantID, Won: r.Won,
		Forfeited: r.Forfeited, Placement: r.Placement, Points: r.Points, RecordedAt: r.RecordedAt,
	}); err != nil {
		return err
	}
	gs.bouts[r.MatchID] = append(gs.bouts[r.MatchID], r)
	tx.boutsRecorded = true
	slices.SortStableFunc(gs.bouts[r.MatchID], func(a, b db.BoutResult) int { return int(a.Bout - b.Bout) })
	return nil
}

// BoutView is a recorded Bout as the API shows it.
type BoutView struct {
	Bout    int32            `json:"bout"`
	Results []BoutResultView `json:"results"`
}

// BoutResultView is one Participant's result in a Bout.
type BoutResultView struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	Won           bool              `json:"won,omitempty"`
	Forfeited     bool              `json:"forfeited,omitempty"`
	Placement     *int32            `json:"placement,omitempty"`
	Points        *int32            `json:"points,omitempty"`
}

// BoutsView groups persisted results into Bouts.
func BoutsView(results []db.BoutResult) []BoutView {
	out := []BoutView{}
	for _, r := range results {
		if len(out) == 0 || out[len(out)-1].Bout != r.Bout {
			out = append(out, BoutView{Bout: r.Bout})
		}
		b := &out[len(out)-1]
		b.Results = append(b.Results, BoutResultView{
			ParticipantID: r.ParticipantID, Won: r.Won, Forfeited: r.Forfeited, Placement: r.Placement, Points: r.Points,
		})
	}
	return out
}

func (tx *Tx) emitBout(gs *groupState, m db.Match, bout int32) error {
	var rs []db.BoutResult
	for _, r := range gs.bouts[m.ID] {
		if r.Bout == bout {
			rs = append(rs, r)
		}
	}
	return tx.Emit(events.BoutRecorded, map[string]any{"matchId": m.ID, "bouts": BoutsView(rs)})
}

// forfeitRemaining forfeits every Bout a Participant hasn't completed in a
// Match. In head-to-head the opponent wins those Bouts until the Match is
// decided, or it's a double Forfeit when the opponent left too. In
// free-for-all every remaining Bout scores last placement and no points.
func (tx *Tx) forfeitRemaining(gs *groupState, m db.Match, p ids.ParticipantID) error {
	if gs.headToHead() {
		opponent := gs.slots[m.ID][0]
		if opponent == p {
			opponent = gs.slots[m.ID][1]
		}
		opponentLeft := gs.isDropped(opponent)
		for !gs.decided(m) {
			n := gs.playedBouts(m) + 1
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: n, ParticipantID: p, Forfeited: true}); err != nil {
				return err
			}
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: n, ParticipantID: opponent, Won: !opponentLeft, Forfeited: opponentLeft}); err != nil {
				return err
			}
			if err := tx.emitBout(gs, m, n); err != nil {
				return err
			}
		}
		return nil
	}
	for b := int32(1); b <= gs.stage.Bouts; b++ {
		if _, ok := gs.boutResults(m, b)[p]; ok {
			continue
		}
		if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: b, ParticipantID: p, Forfeited: true}); err != nil {
			return err
		}
	}
	return nil
}

// Withdraw takes a Participant out of a running Tournament of their own
// accord: they forfeit every Bout they haven't completed.
func (tx *Tx) Withdraw(p ids.ParticipantID) error { return tx.leave(p, lifecycle.Withdrawn) }

// Disqualify has the Organizer remove a Participant from a running
// Tournament: they forfeit every Bout they haven't completed.
func (tx *Tx) Disqualify(p ids.ParticipantID) error { return tx.leave(p, lifecycle.Disqualified) }

// leave takes a Participant out of a running Tournament with a status that
// HasLeft: they forfeit every Bout they haven't completed, including in a
// Match already In Progress, whose Game Server is told.
func (tx *Tx) leave(p ids.ParticipantID, status lifecycle.ParticipantStatus) error {
	person, err := tx.Participant(p)
	if err != nil {
		return err
	}
	if tx.T.Status != lifecycle.Running || person.Status != lifecycle.Active {
		return problem.New(problem.Conflict, CodeParticipantNotActive, "only an active participant of a running tournament can leave it")
	}
	if err := tx.setParticipantStatus(p, status); err != nil {
		return err
	}
	matches, err := tx.Q.ListOpenMatchesOfParticipant(tx.ctx, p)
	if err != nil {
		return err
	}
	for _, m := range matches {
		gs, err := tx.loadGroup(m.GroupID)
		if err != nil {
			return err
		}
		if err := tx.forfeitRemaining(gs, m, p); err != nil {
			return err
		}
		if err := tx.ProgressGroup(m.GroupID); err != nil {
			return err
		}
		if err := tx.notifyForfeits(m.ID); err != nil {
			return err
		}
	}
	// A Participant between Matches (e.g. waiting for a Swiss Round) is left
	// out of what comes next.
	groups, err := tx.Q.ListGroups(tx.ctx, tx.T.ID)
	if err != nil {
		return err
	}
	for _, g := range groups {
		if g.Status == lifecycle.StageRunning {
			if err := tx.ProgressGroup(g.ID); err != nil {
				return err
			}
		}
	}
	return nil
}

// notifyForfeits tells a Match's running Game Server which Participants left.
func (tx *Tx) notifyForfeits(id ids.MatchID) error {
	m, err := tx.Q.GetMatch(tx.ctx, id)
	if err != nil || m.ServerName == nil || !m.Status.Open() {
		return err
	}
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return err
	}
	var left []ids.ParticipantID
	for _, p := range gs.slots[m.ID] {
		if gs.isDropped(p) {
			left = append(left, p)
		}
	}
	server := *m.ServerName
	tx.After(func(ctx context.Context) {
		if err := tx.s.Servers.NotifyForfeits(ctx, server, left); err != nil {
			slog.WarnContext(ctx, "notify game server of forfeits", "server", server, "error", err)
		}
	})
	return nil
}

// Resolution is how the Organizer resolves a Stalled Match.
type Resolution struct {
	// Winner is awarded the Match; every other Participant forfeits.
	Winner ids.ParticipantID
	// DoubleForfeit makes every Participant forfeit.
	DoubleForfeit bool
}

// ResolveStalled is the only manual way to set a result.
func (tx *Tx) ResolveStalled(m db.Match, r Resolution) error {
	if m.Status != lifecycle.MatchStalled {
		return problem.New(problem.Conflict, CodeMatchNotStalled, "only a stalled match can be resolved, this one is %s", m.Status)
	}
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return err
	}
	slots := gs.slots[m.ID]
	if !r.DoubleForfeit && !slices.Contains(slots, r.Winner) {
		return problem.Fields{{Location: "body.winner", Message: "the winner must be a participant of the match", Value: r.Winner}}.Err()
	}
	if gs.headToHead() {
		loser := slots[0]
		if loser == r.Winner {
			loser = slots[1]
		}
		for !gs.decided(m) {
			n := gs.playedBouts(m) + 1
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: n, ParticipantID: loser, Forfeited: true}); err != nil {
				return err
			}
			other := db.BoutResult{MatchID: m.ID, Bout: n, ParticipantID: r.Winner, Won: true}
			if r.DoubleForfeit {
				other = db.BoutResult{MatchID: m.ID, Bout: n, ParticipantID: slots[0], Forfeited: true}
				if loser == slots[0] {
					other.ParticipantID = slots[1]
				}
			}
			if err := tx.insertResult(gs, other); err != nil {
				return err
			}
			if err := tx.emitBout(gs, m, n); err != nil {
				return err
			}
		}
	} else {
		for b := int32(1); b <= gs.stage.Bouts; b++ {
			have := gs.boutResults(m, b)
			for _, p := range slots {
				if _, ok := have[p]; ok {
					continue
				}
				res := db.BoutResult{MatchID: m.ID, Bout: b, ParticipantID: p, Forfeited: true}
				if !r.DoubleForfeit && p == r.Winner {
					res = db.BoutResult{MatchID: m.ID, Bout: b, ParticipantID: p, Placement: ptr[int32](1), Points: ptr[int32](0)}
				}
				if err := tx.insertResult(gs, res); err != nil {
					return err
				}
			}
		}
	}
	return tx.ProgressGroup(m.GroupID)
}

// ExpireMatch flags a Match with no result by its Result Deadline as
// Stalled, and releases its Game Server.
func (s *Service) ExpireMatch(ctx context.Context, id ids.MatchID) error {
	return s.InMatch(ctx, id, func(tx *Tx, m db.Match) error {
		awaitingResult := m.Status.Open() && m.Status != lifecycle.MatchStalled
		if !awaitingResult {
			return nil
		}
		if m.ResultDeadline != nil && m.ResultDeadline.After(tx.now) {
			return nil
		}
		if err := tx.Q.SetMatchStatus(tx.ctx, db.SetMatchStatusParams{ID: m.ID, Status: lifecycle.MatchStalled}); err != nil {
			return err
		}
		if err := tx.Unschedule(lifecycle.JobAllocate, m.ID); err != nil {
			return err
		}
		tx.release(m)
		gs, err := tx.loadGroup(m.GroupID)
		if err != nil {
			return err
		}
		m.Status = lifecycle.MatchStalled
		return tx.EmitMatch(m, gs.slots[m.ID])
	})
}
