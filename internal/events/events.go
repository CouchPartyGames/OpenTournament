// Package events is the live-update outbox (ADR-0003): events are written to
// the events table in the same transaction as the change that caused them,
// numbered per Tournament, and announced with NOTIFY on commit.
package events

import (
	"context"
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
	"time"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/jackc/pgx/v5"
)

// Channel is the NOTIFY channel. Payloads are "<tournament id>:<seq>".
const Channel = "opentournament_events"

// Event types.
const (
	TournamentStatusChanged = "tournament.status-changed"
	TournamentCompleted     = "tournament.completed"
	RegistrationsChanged    = "registrations.changed"
	ParticipantChanged      = "participant.changed"
	StageStarted            = "stage.started"
	StageCompleted          = "stage.completed"
	MatchChanged            = "match.changed"
	BoutRecorded            = "bout.recorded"
	StandingsChanged        = "standings.changed"
)

// Event is one live update.
type Event struct {
	Type string
	Data any
	// Private is sent, merged into Data, only to Recipients.
	Private    any
	Recipients []string
}

// Append writes an event to the outbox and schedules its NOTIFY for commit.
// The caller must hold the Tournament's row lock, which orders sequence
// numbers.
func Append(ctx context.Context, tx pgx.Tx, q *db.Queries, tournament ids.TournamentID, e Event, now time.Time) (int64, error) {
	seq, err := q.NextEventSeq(ctx, tournament)
	if err != nil {
		return 0, fmt.Errorf("next event seq: %w", err)
	}
	data, err := json.Marshal(e.Data)
	if err != nil {
		return 0, err
	}
	var private json.RawMessage
	if e.Private != nil {
		if private, err = json.Marshal(e.Private); err != nil {
			return 0, err
		}
	}
	recipients := e.Recipients
	if recipients == nil {
		recipients = []string{}
	}
	if err := q.InsertEvent(ctx, db.InsertEventParams{
		TournamentID: tournament, Seq: seq, Type: e.Type, Data: data,
		Private: private, Recipients: recipients, CreatedAt: now,
	}); err != nil {
		return 0, fmt.Errorf("insert event: %w", err)
	}
	// NOTIFY inside a transaction is delivered when it commits.
	if _, err := tx.Exec(ctx, "SELECT pg_notify($1, $2)", Channel, Payload(tournament, seq)); err != nil {
		return 0, fmt.Errorf("notify: %w", err)
	}
	return seq, nil
}

// Payload is the NOTIFY payload for an event.
func Payload(tournament ids.TournamentID, seq int64) string {
	return tournament.String() + ":" + strconv.FormatInt(seq, 10)
}

// ParsePayload reads a NOTIFY payload.
func ParsePayload(s string) (ids.TournamentID, int64, error) {
	id, seqText, ok := strings.Cut(s, ":")
	if !ok {
		return ids.TournamentID{}, 0, fmt.Errorf("bad payload %q", s)
	}
	tid, err := ids.Parse[ids.TournamentID](id)
	if err != nil {
		return ids.TournamentID{}, 0, err
	}
	seq, err := strconv.ParseInt(seqText, 10, 64)
	return tid, seq, err
}
