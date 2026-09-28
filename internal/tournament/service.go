// Package tournament is the Tournament aggregate and the Match lifecycle.
// Every change to a Tournament runs in a transaction holding its lock (Tx),
// which orders its live-update events. The package owns the lifecycle that
// several features share: status transitions, Stage generation through the
// Format engine, Match readiness and completion, Forfeits, and Game Server
// allocation. Feature slices add their own rules on top through Tx, such as
// registration's Capacity check.
package tournament

import (
	"context"
	"errors"
	"fmt"
	"math/rand/v2"
	"sync"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/clock"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/events"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/couchpartygames/opentournament/internal/matchtoken"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

// Service runs Tournaments.
type Service struct {
	Pool    *pgxpool.Pool
	Queries *db.Queries
	Clock   clock.Clock
	Games   *games.Catalog
	Tokens  *matchtoken.Issuer
	Servers gameserver.Port
	// Wake is called after a commit that scheduled due work.
	Wake func()

	rngMu sync.Mutex
	rng   *rand.Rand
}

// Deps are what a Service needs.
type Deps struct {
	Pool    *pgxpool.Pool
	Clock   clock.Clock
	Games   *games.Catalog
	Tokens  *matchtoken.Issuer
	Servers gameserver.Port
	// Rand is the source of all randomness (Seeding and Lots); tests pass a
	// seeded one.
	Rand *rand.Rand
}

// NewService returns a Service.
func NewService(d Deps) *Service {
	return &Service{
		Pool: d.Pool, Queries: db.New(d.Pool), Clock: d.Clock, Games: d.Games,
		Tokens: d.Tokens, Servers: d.Servers, Wake: func() {}, rng: d.Rand,
	}
}

func (s *Service) random(fn func(r *rand.Rand)) {
	s.rngMu.Lock()
	defer s.rngMu.Unlock()
	fn(s.rng)
}

// Tx is a transaction on one Tournament, holding its row lock.
type Tx struct {
	ctx    context.Context
	s      *Service
	raw    pgx.Tx
	Q      *db.Queries
	T      db.Tournament
	now    time.Time
	after  []func(context.Context)
	wake   bool
	people map[ids.ParticipantID]db.Participant
	// boutsRecorded makes the next Group progression publish Standings,
	// which change with every Bout in free-for-all.
	boutsRecorded bool
}

// ErrTournamentNotFound is returned for an unknown Tournament.
var ErrTournamentNotFound = problem.New(problem.NotFound, problem.CodeTournamentNotFound, "no such tournament")

// InTournament runs fn in a transaction holding the Tournament's lock. Every
// change to a Tournament goes through here, which orders its events. Work
// registered with After runs once the transaction has committed.
func (s *Service) InTournament(ctx context.Context, id ids.TournamentID, fn func(tx *Tx) error) error {
	return s.inTx(ctx, id, nil, fn)
}

// Create inserts a new Tournament and runs fn on it, in one transaction.
func (s *Service) Create(ctx context.Context, t db.InsertTournamentParams, fn func(tx *Tx) error) error {
	return s.inTx(ctx, t.ID, &t, fn)
}

func (s *Service) inTx(ctx context.Context, id ids.TournamentID, insert *db.InsertTournamentParams, fn func(tx *Tx) error) error {
	raw, err := s.Pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("begin: %w", err)
	}
	defer raw.Rollback(context.WithoutCancel(ctx))
	q := s.Queries.WithTx(raw)
	if insert != nil {
		if err := q.InsertTournament(ctx, *insert); err != nil {
			return fmt.Errorf("insert tournament: %w", err)
		}
	}
	t, err := q.LockTournament(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return ErrTournamentNotFound
	} else if err != nil {
		return fmt.Errorf("lock tournament: %w", err)
	}
	tx := &Tx{ctx: ctx, s: s, raw: raw, Q: q, T: t, now: s.Clock.Now()}
	if err := fn(tx); err != nil {
		return err
	}
	if err := raw.Commit(ctx); err != nil {
		return fmt.Errorf("commit: %w", err)
	}
	bg := context.WithoutCancel(ctx)
	for _, f := range tx.after {
		f(bg)
	}
	if tx.wake {
		s.Wake()
	}
	return nil
}

// Game returns the Game the Tournament is played in. It fails if the Game
// was removed from the catalog after the Tournament was created.
func (tx *Tx) Game() (games.Game, error) {
	g, ok := tx.s.Games.Get(tx.T.GameID)
	if !ok {
		return games.Game{}, fmt.Errorf("game %q is no longer in the catalog", tx.T.GameID)
	}
	return g, nil
}

// RequireOrganizer fails unless the caller is the Tournament's Organizer.
func (tx *Tx) RequireOrganizer(p auth.Principal) error {
	if tx.T.Organizer != p.ID() {
		return problem.New(problem.Forbidden, problem.CodeNotOrganizer, "only the tournament's organizer can do this")
	}
	return nil
}

// Ctx is the transaction's context.
func (tx *Tx) Ctx() context.Context { return tx.ctx }

// Now is the time the transaction started.
func (tx *Tx) Now() time.Time { return tx.now }

// Service returns the Service the transaction belongs to.
func (tx *Tx) Service() *Service { return tx.s }

// After runs f once the transaction has committed.
func (tx *Tx) After(f func(ctx context.Context)) { tx.after = append(tx.after, f) }

// Emit writes a public live-update event.
func (tx *Tx) Emit(typ string, data any) error {
	return tx.EmitEvent(events.Event{Type: typ, Data: data})
}

// EmitEvent writes a live-update event.
func (tx *Tx) EmitEvent(e events.Event) error {
	_, err := events.Append(tx.ctx, tx.raw, tx.Q, tx.T.ID, e, tx.now)
	return err
}

// Schedule persists a due time for background work.
func (tx *Tx) Schedule(kind string, match ids.MatchID, due time.Time) error {
	tx.wake = true
	return tx.Q.ScheduleJob(tx.ctx, db.ScheduleJobParams{Kind: kind, TournamentID: tx.T.ID, MatchID: match, DueAt: due})
}

// Unschedule drops pending background work.
func (tx *Tx) Unschedule(kind string, match ids.MatchID) error {
	return tx.Q.UnscheduleJob(tx.ctx, db.UnscheduleJobParams{Kind: kind, TournamentID: tx.T.ID, MatchID: match})
}

// Participant returns one of the Tournament's Participants.
func (tx *Tx) Participant(id ids.ParticipantID) (db.Participant, error) {
	if err := tx.loadPeople(); err != nil {
		return db.Participant{}, err
	}
	p, ok := tx.people[id]
	if !ok {
		return db.Participant{}, problem.New(problem.NotFound, problem.CodeParticipantNotFound, "no such participant in this tournament")
	}
	return p, nil
}

func (tx *Tx) loadPeople() error {
	if tx.people != nil {
		return nil
	}
	ps, err := tx.Q.ListParticipants(tx.ctx, tx.T.ID)
	if err != nil {
		return err
	}
	tx.people = make(map[ids.ParticipantID]db.Participant, len(ps))
	for _, p := range ps {
		tx.people[p.ID] = p
	}
	return nil
}

// setParticipantStatus changes a Participant's status and announces it.
func (tx *Tx) setParticipantStatus(id ids.ParticipantID, status string) error {
	if err := tx.loadPeople(); err != nil {
		return err
	}
	if err := tx.Q.SetParticipantStatus(tx.ctx, db.SetParticipantStatusParams{ID: id, Status: status}); err != nil {
		return err
	}
	p := tx.people[id]
	p.Status = status
	tx.people[id] = p
	return tx.Emit(events.ParticipantChanged, map[string]any{"participantId": id, "status": status})
}

// SetStatus moves the Tournament to a new status and announces it.
func (tx *Tx) SetStatus(status string) error {
	if err := tx.Q.SetTournamentStatus(tx.ctx, db.SetTournamentStatusParams{ID: tx.T.ID, Status: status, UpdatedAt: tx.now}); err != nil {
		return err
	}
	tx.T.Status = status
	return tx.Emit(events.TournamentStatusChanged, map[string]any{"status": status})
}
