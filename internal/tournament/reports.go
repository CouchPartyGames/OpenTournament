package tournament

import (
	"context"
	"fmt"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/matchtoken"
	"github.com/couchpartygames/opentournament/internal/problem"
)

var errTokenInvalid = problem.New(problem.Unauthenticated, CodeMatchTokenInvalid,
	"the match token is invalid, expired, or no longer belongs to this match's game server")

// AsGameServer runs fn for the Game Server holding a Match token. The token
// is only good for its own Match, on its own allocation, while the Match is
// being played.
func (s *Service) AsGameServer(ctx context.Context, raw string, fn func(tx *Tx, m db.Match) error) error {
	claims, err := s.Tokens.Verify(raw)
	if err != nil {
		return errTokenInvalid
	}
	err = s.InMatch(ctx, claims.MatchID, func(tx *Tx, m db.Match) error {
		if !servedBy(m, claims, tx.now) {
			return errTokenInvalid
		}
		return fn(tx, m)
	})
	if err == ErrMatchNotFound {
		return errTokenInvalid
	}
	return err
}

// servedBy reports whether a token belongs to the Game Server a Match is
// played on. A Stalled Match still hears from its server until the token
// expires, so a result sent just before the Result Deadline isn't lost.
func servedBy(m db.Match, c matchtoken.Claims, now time.Time) bool {
	if m.AllocationID != c.AllocationID {
		return false
	}
	if m.Status == lifecycle.MatchStalled {
		return m.ResultDeadline != nil && !now.After(m.ResultDeadline.Add(matchtoken.Grace))
	}
	return m.Status.Playing()
}

// ServerMatchView is what a Game Server needs to set up its session.
type ServerMatchView struct {
	MatchID      ids.MatchID             `json:"matchId"`
	TournamentID ids.TournamentID        `json:"tournamentId"`
	GameID       string                  `json:"gameId"`
	Status       lifecycle.MatchStatus   `json:"status"`
	Format       string                  `json:"format"`
	BestOf       int32                   `json:"bestOf,omitempty" doc:"Head-to-head only: 1 or 3"`
	Bouts        int32                   `json:"bouts,omitempty" doc:"Free-for-all only: the number of Bouts"`
	Participants []ServerParticipantView `json:"participants"`
	// CompletedBouts lets a replacement server resume after an Abort.
	CompletedBouts []BoutView `json:"completedBouts"`
}

// ServerParticipantView is a Participant as its Game Server sees it.
type ServerParticipantView struct {
	ParticipantID ids.ParticipantID `json:"participantId"`
	IdentityKind  string            `json:"identityKind"`
	IdentityValue string            `json:"identityValue"`
	Forfeited     bool              `json:"forfeited" doc:"Withdrew or was disqualified: kick them and don't wait for them"`
}

// ServerMatch describes a Match to its Game Server.
func (tx *Tx) ServerMatch(m db.Match) (ServerMatchView, error) {
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return ServerMatchView{}, err
	}
	v := ServerMatchView{
		MatchID: m.ID, TournamentID: m.TournamentID, GameID: tx.T.GameID, Status: m.Status,
		Format: gs.stage.Format, CompletedBouts: BoutsView(gs.bouts[m.ID]),
	}
	if gs.headToHead() {
		v.BestOf = gs.stage.BestOf
	} else {
		v.Bouts = gs.stage.Bouts
	}
	for _, p := range gs.slots[m.ID] {
		person, err := tx.Participant(p)
		if err != nil {
			return ServerMatchView{}, err
		}
		v.Participants = append(v.Participants, ServerParticipantView{
			ParticipantID: p, IdentityKind: person.IdentityKind, IdentityValue: person.IdentityValue,
			Forfeited: person.Status.HasLeft(),
		})
	}
	return v, nil
}

// ReportStarted marks the Match In Progress.
func (tx *Tx) ReportStarted(m db.Match) error {
	if m.Status == lifecycle.MatchInProgress || m.Status == lifecycle.MatchStalled {
		return nil
	}
	if _, err := tx.Q.MarkMatchStarted(tx.ctx, db.MarkMatchStartedParams{ID: m.ID, AllocationID: m.AllocationID, StartedAt: &tx.now}); err != nil {
		return err
	}
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return err
	}
	m.Status = lifecycle.MatchInProgress
	return tx.EmitMatch(m, gs.slots[m.ID])
}

// PlacementReport is one Participant's result in a free-for-all Bout.
type PlacementReport struct {
	ParticipantID ids.ParticipantID
	Placement     int32
	Points        int32
}

// BoutReport is a Game Server's result for one Bout: a winner for
// head-to-head, or placements and points for free-for-all.
type BoutReport struct {
	Bout       int32
	Winner     ids.ParticipantID
	Placements []PlacementReport
}

func errBoutConflict() error {
	return problem.New(problem.Conflict, CodeBoutConflict, "a different result is already recorded for this bout")
}

func errForfeited() error {
	return problem.New(problem.Conflict, CodeBoutAlreadyForfeited, "this bout was already forfeited")
}

func errOutOfOrder(next int32) error {
	return problem.New(problem.Conflict, CodeBoutOutOfOrder, "the next bout to report is bout %d", next)
}

// ReportBout records a Bout's result. Repeating an identical report is a
// no-op; a different one conflicts. A Bout already forfeited stays forfeited.
func (tx *Tx) ReportBout(m db.Match, r BoutReport) error {
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return err
	}
	slots := gs.slots[m.ID]
	have := gs.boutResults(m, r.Bout)
	next := gs.playedBouts(m) + 1

	if gs.headToHead() {
		if !slices.Contains(slots, r.Winner) {
			return problem.Fields{{Location: "body.winner", Message: "the winner must be a participant of the match", Value: r.Winner}}.Err()
		}
		if len(have) > 0 {
			for _, res := range have {
				if res.Forfeited {
					return errForfeited()
				}
			}
			if have[r.Winner].Won {
				return nil
			}
			return errBoutConflict()
		}
		if r.Bout != next {
			return errOutOfOrder(next)
		}
		for _, p := range slots {
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: r.Bout, ParticipantID: p, Won: p == r.Winner}); err != nil {
				return err
			}
		}
	} else {
		if err := tx.checkPlacements(gs, m, r, have, next); err != nil || len(have) == len(slots) {
			return err
		}
		for _, pl := range r.Placements {
			if err := tx.insertResult(gs, db.BoutResult{
				MatchID: m.ID, Bout: r.Bout, ParticipantID: pl.ParticipantID, Placement: ptr(pl.Placement), Points: ptr(pl.Points),
			}); err != nil {
				return err
			}
		}
	}
	if err := tx.emitBout(gs, m, r.Bout); err != nil {
		return err
	}
	return tx.ProgressGroup(m.GroupID)
}

// checkPlacements validates a free-for-all report. It returns nil without
// anything to record when an identical report is repeated.
func (tx *Tx) checkPlacements(gs *groupState, m db.Match, r BoutReport, have map[ids.ParticipantID]db.BoutResult, next int32) error {
	slots := gs.slots[m.ID]
	var fields problem.Fields
	if r.Bout > gs.stage.Bouts {
		fields.Add("path.bout", "the match has fewer bouts", r.Bout)
	}
	seen := map[ids.ParticipantID]bool{}
	places := map[int32]bool{}
	for i, pl := range r.Placements {
		switch {
		case !slices.Contains(slots, pl.ParticipantID):
			fields.Add(fmt.Sprintf("body.placements[%d].participantId", i), "not a participant of the match", pl.ParticipantID)
		case seen[pl.ParticipantID]:
			fields.Add(fmt.Sprintf("body.placements[%d].participantId", i), "reported twice", pl.ParticipantID)
		}
		if places[pl.Placement] || pl.Placement < 1 || int(pl.Placement) > len(slots) {
			fields.Add(fmt.Sprintf("body.placements[%d].placement", i), "placements must be distinct, from 1 to the number of participants", pl.Placement)
		}
		seen[pl.ParticipantID], places[pl.Placement] = true, true
	}
	if err := fields.Err(); err != nil {
		return err
	}
	for _, pl := range r.Placements {
		if res, ok := have[pl.ParticipantID]; ok {
			if res.Forfeited {
				return errForfeited()
			}
			if len(have) == len(slots) && *res.Placement == pl.Placement && *res.Points == pl.Points {
				continue
			}
			return errBoutConflict()
		}
	}
	if len(have) == len(slots) {
		if len(r.Placements) == len(slots)-countForfeits(have) {
			return nil
		}
		return errBoutConflict()
	}
	if r.Bout != next {
		return errOutOfOrder(next)
	}
	for _, p := range slots {
		if _, ok := have[p]; !ok && !seen[p] {
			if gs.isDropped(p) {
				continue
			}
			return problem.Fields{{Location: "body.placements", Message: "every participant still playing needs a placement", Value: p}}.Err()
		}
		if seen[p] && gs.isDropped(p) {
			return errForfeited()
		}
	}
	return nil
}

func countForfeits(rs map[ids.ParticipantID]db.BoutResult) int {
	n := 0
	for _, r := range rs {
		if r.Forfeited {
			n++
		}
	}
	return n
}

// ReportNoShows records that Participants didn't turn up for a Bout: they
// forfeit it. When both head-to-head Participants are No-shows it's a double
// Forfeit.
func (tx *Tx) ReportNoShows(m db.Match, bout int32, who []ids.ParticipantID) error {
	gs, err := tx.loadGroup(m.GroupID)
	if err != nil {
		return err
	}
	slots := gs.slots[m.ID]
	var fields problem.Fields
	for i, p := range who {
		if !slices.Contains(slots, p) {
			fields.Add(fmt.Sprintf("body.participantIds[%d]", i), "not a participant of the match", p)
		}
	}
	if !gs.headToHead() && bout > gs.stage.Bouts {
		fields.Add("path.bout", "the match has fewer bouts", bout)
	}
	if err := fields.Err(); err != nil {
		return err
	}
	have := gs.boutResults(m, bout)
	next := gs.playedBouts(m) + 1

	if gs.headToHead() {
		if len(have) > 0 {
			for _, p := range slots {
				if have[p].Forfeited != slices.Contains(who, p) {
					return errBoutConflict()
				}
			}
			return nil
		}
		if bout != next {
			return errOutOfOrder(next)
		}
		both := slices.Contains(who, slots[0]) && slices.Contains(who, slots[1])
		for _, p := range slots {
			noShow := slices.Contains(who, p)
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: bout, ParticipantID: p, Forfeited: noShow, Won: !noShow && !both}); err != nil {
				return err
			}
		}
	} else {
		if bout != next {
			return errOutOfOrder(next)
		}
		for _, p := range who {
			if res, ok := have[p]; ok {
				if res.Forfeited {
					continue
				}
				return errBoutConflict()
			}
			if err := tx.insertResult(gs, db.BoutResult{MatchID: m.ID, Bout: bout, ParticipantID: p, Forfeited: true}); err != nil {
				return err
			}
		}
	}
	if err := tx.emitBout(gs, m, bout); err != nil {
		return err
	}
	return tx.ProgressGroup(m.GroupID)
}
