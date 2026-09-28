// Package scheduler runs time-based and retried work from persisted due
// times, so a restart loses nothing. Several replicas can run it: each claims
// due jobs with SELECT … FOR UPDATE SKIP LOCKED and leases them.
package scheduler

import (
	"context"
	"errors"
	"log/slog"
	"sync"
	"time"

	"github.com/couchpartygames/opentournament/internal/clock"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/jackc/pgx/v5"
)

// Handler performs one job. Handlers must be idempotent: a job can run again
// after a crash.
type Handler func(ctx context.Context, job db.Job) error

// Scheduler claims and runs due jobs.
type Scheduler struct {
	q        *db.Queries
	clock    clock.Clock
	handlers map[string]Handler
	wake     chan struct{}

	// Lease is how long a claimed job is reserved for its worker.
	Lease time.Duration
	// Poll is the longest the loop sleeps between checks.
	Poll time.Duration
	// Retention is how long live-update events are kept.
	Retention time.Duration
	// Concurrency is how many claimed jobs run at once. Tournaments start in
	// bursts: every first-Round Match needs a Game Server at the same moment.
	Concurrency int
}

// New returns a Scheduler.
func New(q *db.Queries, c clock.Clock, handlers map[string]Handler) *Scheduler {
	return &Scheduler{
		q: q, clock: c, handlers: handlers, wake: make(chan struct{}, 1),
		Lease: time.Minute, Poll: time.Second, Retention: time.Hour, Concurrency: 16,
	}
}

// Wake makes the loop look for due jobs now.
func (s *Scheduler) Wake() {
	select {
	case s.wake <- struct{}{}:
	default:
	}
}

// Backoff is the delay before retry number attempt: 1s, 2s, 4s, … up to a minute.
func Backoff(attempt int32) time.Duration {
	d := time.Second << max(0, min(attempt-1, 6))
	return min(d, time.Minute)
}

// RunDue runs every job that is due now, and returns how many ran.
func (s *Scheduler) RunDue(ctx context.Context) (int, error) {
	ran := 0
	for {
		now := s.clock.Now()
		jobs, err := s.q.ClaimDueJobs(ctx, db.ClaimDueJobsParams{Now: now, LeaseUntil: now.Add(s.Lease), MaxJobs: 32})
		if err != nil {
			return ran, err
		}
		if len(jobs) == 0 {
			return ran, nil
		}
		var wg sync.WaitGroup
		slots := make(chan struct{}, max(1, s.Concurrency))
		for _, job := range jobs {
			slots <- struct{}{}
			wg.Go(func() {
				defer func() { <-slots }()
				s.run(ctx, job)
			})
		}
		wg.Wait()
		ran += len(jobs)
	}
}

func (s *Scheduler) run(ctx context.Context, job db.Job) {
	log := slog.With("job", job.Kind, "tournament", job.TournamentID, "attempt", job.Attempts)
	if !job.MatchID.IsZero() {
		log = log.With("match", job.MatchID)
	}
	handler, ok := s.handlers[job.Kind]
	var err error
	if !ok {
		log.ErrorContext(ctx, "no handler for job; dropping it")
	} else {
		err = handler(ctx, job)
	}
	if err == nil {
		if err := s.q.FinishJob(ctx, db.FinishJobParams{ID: job.ID, DueAt: job.DueAt}); err != nil {
			log.ErrorContext(ctx, "finish job", "error", err)
		}
		return
	}
	retry := s.clock.Now().Add(Backoff(job.Attempts))
	log.WarnContext(ctx, "job failed; retrying", "error", err, "retry_at", retry)
	if err := s.q.RetryJob(ctx, db.RetryJobParams{ID: job.ID, LeasedUntil: job.DueAt, RetryAt: retry}); err != nil {
		log.ErrorContext(ctx, "reschedule job", "error", err)
	}
}

// Run runs due jobs until ctx ends, and prunes old live-update events.
func (s *Scheduler) Run(ctx context.Context) {
	var lastPrune time.Time
	for {
		if _, err := s.RunDue(ctx); err != nil && ctx.Err() == nil {
			slog.ErrorContext(ctx, "run due jobs", "error", err)
		}
		if now := s.clock.Now(); now.Sub(lastPrune) > time.Minute {
			lastPrune = now
			if _, err := s.q.PruneEvents(ctx, now.Add(-s.Retention)); err != nil && ctx.Err() == nil {
				slog.ErrorContext(ctx, "prune events", "error", err)
			}
		}
		wait := s.Poll
		if next, err := s.q.NextJobDue(ctx); err == nil {
			wait = min(wait, max(0, next.Sub(s.clock.Now())))
		} else if !errors.Is(err, pgx.ErrNoRows) && ctx.Err() == nil {
			slog.ErrorContext(ctx, "next job due", "error", err)
		}
		timer := time.NewTimer(wait)
		select {
		case <-ctx.Done():
			timer.Stop()
			return
		case <-s.wake:
			timer.Stop()
		case <-timer.C:
		}
	}
}
