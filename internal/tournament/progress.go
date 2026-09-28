package tournament

import (
	"context"
	"fmt"
	"log/slog"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
)

// groupState is everything persisted about one Group.
type groupState struct {
	group        db.Group
	stage        db.Stage
	participants []db.ListGroupParticipantsRow
	matches      map[string]db.Match
	slots        map[ids.MatchID][]ids.ParticipantID
	bouts        map[ids.MatchID][]db.BoutResult
}

func (tx *Tx) loadGroup(id ids.GroupID) (*groupState, error) {
	g, err := tx.Q.GetGroup(tx.ctx, id)
	if err != nil {
		return nil, fmt.Errorf("group: %w", err)
	}
	stages, err := tx.Q.ListStages(tx.ctx, tx.T.ID)
	if err != nil {
		return nil, err
	}
	gs := &groupState{
		group:   g,
		matches: map[string]db.Match{},
		slots:   map[ids.MatchID][]ids.ParticipantID{},
		bouts:   map[ids.MatchID][]db.BoutResult{},
	}
	for _, st := range stages {
		if st.ID == g.StageID {
			gs.stage = st
		}
	}
	if gs.participants, err = tx.Q.ListGroupParticipants(tx.ctx, id); err != nil {
		return nil, err
	}
	ms, err := tx.Q.ListMatchesOfGroup(tx.ctx, id)
	if err != nil {
		return nil, err
	}
	for _, m := range ms {
		gs.matches[m.Key] = m
	}
	slots, err := tx.Q.ListSlotsOfGroup(tx.ctx, id)
	if err != nil {
		return nil, err
	}
	for _, s := range slots {
		gs.slots[s.MatchID] = append(gs.slots[s.MatchID], s.ParticipantID)
	}
	bouts, err := tx.Q.ListBoutsOfGroup(tx.ctx, id)
	if err != nil {
		return nil, err
	}
	for _, b := range bouts {
		gs.bouts[b.MatchID] = append(gs.bouts[b.MatchID], b)
	}
	return gs, nil
}

// StageRules is the part of a Stage the Format engine needs.
func StageRules(st db.Stage) format.Stage {
	return format.Stage{
		Format:      format.Kind(st.Format),
		BestOf:      int(st.BestOf),
		Bouts:       int(st.Bouts),
		SwissRounds: int(st.SwissRounds),
	}
}

func pid(id ids.ParticipantID) format.ParticipantID { return format.ParticipantID(id.String()) }

// ParticipantOf converts an engine Participant back to its ID.
func ParticipantOf(p format.ParticipantID) ids.ParticipantID {
	id, _ := ids.Parse[ids.ParticipantID](string(p))
	return id
}

// EngineBouts groups persisted Bout results into engine Bouts.
func EngineBouts(results []db.BoutResult) []format.Bout {
	var out []format.Bout
	for _, r := range results {
		if len(out) == 0 || out[len(out)-1].Number != int(r.Bout) {
			out = append(out, format.Bout{Number: int(r.Bout)})
		}
		b := &out[len(out)-1]
		br := format.BoutResult{Participant: pid(r.ParticipantID), Won: r.Won, Forfeited: r.Forfeited}
		if r.Placement != nil {
			br.Placement = int(*r.Placement)
		}
		if r.Points != nil {
			br.Points = int(*r.Points)
		}
		b.Results = append(b.Results, br)
	}
	return out
}

func (gs *groupState) engine() format.Group {
	g := format.Group{Stage: StageRules(gs.stage)}
	for _, e := range gs.participants {
		g.Participants = append(g.Participants, format.Participant{ID: pid(e.ParticipantID), Lot: int(e.Lot)})
		if e.Status.HasLeft() {
			g.Dropped = append(g.Dropped, pid(e.ParticipantID))
		}
	}
	for key, m := range gs.matches {
		fm := format.Match{Key: key, Round: int(m.Round), Bouts: EngineBouts(gs.bouts[m.ID])}
		for _, p := range gs.slots[m.ID] {
			fm.Participants = append(fm.Participants, pid(p))
		}
		g.Matches = append(g.Matches, fm)
	}
	slices.SortFunc(g.Matches, func(a, b format.Match) int {
		if a.Round != b.Round {
			return a.Round - b.Round
		}
		if a.Key < b.Key {
			return -1
		}
		return 1
	})
	return g
}

func (gs *groupState) plan() (format.Plan, error) { return format.PlanGroup(gs.engine()) }

// ProgressGroup brings a Group's persisted Matches in line with the Format
// engine's plan: it creates Matches, fills in Participants, makes Matches
// Ready, and completes the ones their Bouts decide. It repeats until nothing
// changes, since making a Match Ready can forfeit Participants who already
// left, which can decide it at once.
func (tx *Tx) ProgressGroup(id ids.GroupID) error {
	completedAny := false
	for range 100 {
		gs, err := tx.loadGroup(id)
		if err != nil {
			return err
		}
		plan, err := gs.plan()
		if err != nil {
			return err
		}
		again, completed, err := tx.applyPlan(gs, plan)
		if err != nil {
			return err
		}
		completedAny = completedAny || completed
		if again {
			continue
		}
		if completedAny || tx.boutsRecorded {
			tx.boutsRecorded = false
			if err := tx.emitStandings(gs, plan); err != nil {
				return err
			}
		}
		if plan.Complete && gs.group.Status == StageRunning {
			if err := tx.Q.SetGroupStatus(tx.ctx, db.SetGroupStatusParams{ID: id, Status: StageCompleted}); err != nil {
				return err
			}
			return tx.finishStageIfComplete(gs.stage)
		}
		return nil
	}
	return fmt.Errorf("group %s did not settle", id)
}

func (tx *Tx) applyPlan(gs *groupState, plan format.Plan) (again, completed bool, err error) {
	for _, pm := range plan.Matches {
		participants := make([]ids.ParticipantID, len(pm.Participants))
		for i, p := range pm.Participants {
			participants[i] = ParticipantOf(p)
		}
		m, exists := gs.matches[pm.Key]
		if !exists {
			m = db.Match{
				ID: ids.New[ids.MatchID](), TournamentID: tx.T.ID, StageID: gs.stage.ID, GroupID: gs.group.ID,
				Key: pm.Key, Round: int32(pm.Round), Bracket: string(pm.Bracket), Status: lifecycle.MatchPending,
			}
			if err := tx.Q.InsertMatch(tx.ctx, db.InsertMatchParams{
				ID: m.ID, TournamentID: m.TournamentID, StageID: m.StageID, GroupID: m.GroupID,
				Key: m.Key, Round: m.Round, Bracket: m.Bracket,
			}); err != nil {
				return false, false, fmt.Errorf("insert match: %w", err)
			}
		}
		if m.Status == lifecycle.MatchPending && !slices.Equal(gs.slots[m.ID], participants) {
			if err := tx.setSlots(m.ID, participants); err != nil {
				return false, false, err
			}
			gs.slots[m.ID] = participants
		}
		switch pm.State {
		case format.Playable:
			if m.Status != lifecycle.MatchPending {
				continue
			}
			if err := tx.makeReady(gs, m); err != nil {
				return false, false, err
			}
			for _, p := range participants {
				person, err := tx.Participant(p)
				if err != nil {
					return false, false, err
				}
				if person.Status.HasLeft() {
					m.Status = lifecycle.MatchReady
					if err := tx.forfeitRemaining(gs, m, p); err != nil {
						return false, false, err
					}
					again = true
				}
			}
		case format.Decided:
			if !m.Status.Open() && m.Status != lifecycle.MatchPending {
				continue
			}
			result, winner := lifecycle.ResultWin, ParticipantOf(pm.Outcome.Winner)
			switch {
			case pm.Outcome.DoubleForfeit:
				result = lifecycle.ResultDoubleForfeit
			case gs.stage.Format == string(format.FreeForAll):
				result = lifecycle.ResultFreeForAll
			}
			if err := tx.completeMatch(gs, m, result, winner); err != nil {
				return false, false, err
			}
			completed = true
		case format.Bye, format.Empty:
			if m.Status != lifecycle.MatchPending {
				continue
			}
			result, winner := lifecycle.ResultEmpty, ids.ParticipantID{}
			if pm.State == format.Bye {
				result, winner = lifecycle.ResultBye, ParticipantOf(pm.Outcome.Winner)
			}
			if err := tx.completeMatch(gs, m, result, winner); err != nil {
				return false, false, err
			}
			completed = true
		}
	}
	return again, completed, nil
}

func (tx *Tx) setSlots(match ids.MatchID, participants []ids.ParticipantID) error {
	if err := tx.Q.DeleteSlots(tx.ctx, match); err != nil {
		return err
	}
	for i, p := range participants {
		if err := tx.Q.InsertSlot(tx.ctx, db.InsertSlotParams{MatchID: match, Slot: int32(i), ParticipantID: p}); err != nil {
			return fmt.Errorf("insert slot: %w", err)
		}
	}
	return nil
}

// makeReady opens a Match for play: its Result Deadline starts running and a
// Game Server is requested.
func (tx *Tx) makeReady(gs *groupState, m db.Match) error {
	deadline := tx.now.Add(time.Duration(gs.stage.ResultDeadlineSeconds) * time.Second)
	if err := tx.Q.SetMatchReady(tx.ctx, db.SetMatchReadyParams{ID: m.ID, ReadyAt: &tx.now, ResultDeadline: &deadline}); err != nil {
		return err
	}
	if err := tx.Schedule(JobAllocate, m.ID, tx.now); err != nil {
		return err
	}
	if err := tx.Schedule(JobResultDeadline, m.ID, deadline); err != nil {
		return err
	}
	m.Status, m.ReadyAt, m.ResultDeadline = lifecycle.MatchReady, &tx.now, &deadline
	return tx.EmitMatch(m, gs.slots[m.ID])
}

// completeMatch records how a Match ended and releases its Game Server.
func (tx *Tx) completeMatch(gs *groupState, m db.Match, result lifecycle.MatchResult, winner ids.ParticipantID) error {
	if err := tx.Q.CompleteMatch(tx.ctx, db.CompleteMatchParams{
		ID: m.ID, Result: &result, WinnerID: winner, CompletedAt: &tx.now,
	}); err != nil {
		return err
	}
	if err := tx.Unschedule(JobAllocate, m.ID); err != nil {
		return err
	}
	if err := tx.Unschedule(JobResultDeadline, m.ID); err != nil {
		return err
	}
	tx.release(m)
	m.Status, m.Result, m.WinnerID, m.CompletedAt = lifecycle.MatchCompleted, &result, winner, &tx.now
	return tx.EmitMatch(m, gs.slots[m.ID])
}

// release gives a Match's Game Server back once the transaction commits. If
// that fails, reconciliation releases it later.
func (tx *Tx) release(m db.Match) {
	if m.ServerName == nil {
		return
	}
	name := *m.ServerName
	tx.After(func(ctx context.Context) {
		if err := tx.s.Servers.Release(ctx, name); err != nil {
			slog.WarnContext(ctx, "release game server", "server", name, "match", m.ID, "error", err)
		}
	})
}

// MatchEvent is the public part of a match.changed event.
type MatchEvent struct {
	MatchID      ids.MatchID            `json:"matchId"`
	StageID      ids.StageID            `json:"stageId"`
	GroupID      ids.GroupID            `json:"groupId"`
	Key          string                 `json:"key"`
	Round        int32                  `json:"round"`
	Status       lifecycle.MatchStatus  `json:"status"`
	Result       *lifecycle.MatchResult `json:"result,omitempty"`
	WinnerID     *ids.ParticipantID     `json:"winnerId,omitempty"`
	Participants []ids.ParticipantID    `json:"participants"`
	// ServerAllocated says a Game Server is assigned, without saying where.
	ServerAllocated bool `json:"serverAllocated"`
	// Aborted is set on the event announcing that the Match's Game Server
	// failed mid-play; the Match goes back to Allocating.
	Aborted bool  `json:"aborted,omitempty"`
	Aborts  int32 `json:"aborts"`
}

// ServerEvent is the private part of a match.changed event.
type ServerEvent struct {
	ServerAddress string `json:"serverAddress"`
	ServerPort    int32  `json:"serverPort"`
}

// EmitMatch announces a Match's state. The Game Server address goes only to
// the Match's Participants and the Organizer.
func (tx *Tx) EmitMatch(m db.Match, participants []ids.ParticipantID) error {
	return tx.emitMatch(m, participants, false)
}

func (tx *Tx) emitMatch(m db.Match, participants []ids.ParticipantID, aborted bool) error {
	data := MatchEvent{
		MatchID: m.ID, StageID: m.StageID, GroupID: m.GroupID, Key: m.Key, Round: m.Round,
		Status: m.Status, Result: m.Result, Participants: participants,
		ServerAllocated: m.ServerName != nil && m.Status.Open(),
		Aborted:         aborted, Aborts: m.Aborts,
	}
	if data.Participants == nil {
		data.Participants = []ids.ParticipantID{}
	}
	if !m.WinnerID.IsZero() {
		w := m.WinnerID
		data.WinnerID = &w
	}
	e := events.Event{Type: events.MatchChanged, Data: data}
	if data.ServerAllocated && m.ServerAddress != nil && m.ServerPort != nil {
		e.Private = ServerEvent{ServerAddress: *m.ServerAddress, ServerPort: *m.ServerPort}
		recipients, err := tx.MatchAudience(participants)
		if err != nil {
			return err
		}
		e.Recipients = recipients
	}
	return tx.EmitEvent(e)
}

// MatchAudience are the recipient keys allowed to see a Match's Game Server:
// its Participants and the Organizer.
func (tx *Tx) MatchAudience(participants []ids.ParticipantID) ([]string, error) {
	out := []string{tx.T.Organizer}
	for _, p := range participants {
		person, err := tx.Participant(p)
		if err != nil {
			return nil, err
		}
		out = append(out, auth.Identity{Kind: person.IdentityKind, Value: person.IdentityValue}.Key())
	}
	return out, nil
}

func (tx *Tx) emitStandings(gs *groupState, plan format.Plan) error {
	return tx.Emit(events.StandingsChanged, map[string]any{
		"groupId":   gs.group.ID,
		"complete":  plan.Complete,
		"standings": StandingsView(plan.Standings),
	})
}
