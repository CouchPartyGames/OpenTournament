// Package reconciler drives the level-triggered reconcile loop between
// Matches in Postgres and Game Servers (ADR-0001). It runs at startup, on
// every Game Server event, and on a periodic resync, through a rate-limited
// client-go workqueue that coalesces bursts of events.
package reconciler

import (
	"context"
	"log/slog"
	"time"

	"k8s.io/client-go/util/workqueue"
)

const key = "all"

// Runner runs a reconcile function.
type Runner struct {
	reconcile func(context.Context) error
	watch     func(context.Context, func()) error
	queue     workqueue.TypedRateLimitingInterface[string]
	// Resync is the period of full reconciles when nothing happens.
	Resync time.Duration
}

// New returns a Runner that calls reconcile, and is triggered by watch.
func New(reconcile func(context.Context) error, watch func(context.Context, func()) error) *Runner {
	return &Runner{
		reconcile: reconcile,
		watch:     watch,
		queue: workqueue.NewTypedRateLimitingQueueWithConfig(
			workqueue.DefaultTypedControllerRateLimiter[string](),
			workqueue.TypedRateLimitingQueueConfig[string]{Name: "reconcile"}),
		Resync: 30 * time.Second,
	}
}

// Trigger asks for a reconcile.
func (r *Runner) Trigger() { r.queue.Add(key) }

// Run reconciles until ctx ends. The first reconcile runs at once, so a
// Game Server that died while the service was down is detected.
func (r *Runner) Run(ctx context.Context) {
	go func() {
		<-ctx.Done()
		r.queue.ShutDown()
	}()
	go func() {
		if err := r.watch(ctx, r.Trigger); err != nil && ctx.Err() == nil {
			slog.ErrorContext(ctx, "watch game servers", "error", err)
		}
	}()
	go func() {
		t := time.NewTicker(r.Resync)
		defer t.Stop()
		for {
			select {
			case <-ctx.Done():
				return
			case <-t.C:
				r.Trigger()
			}
		}
	}()
	r.Trigger()
	for {
		k, shutdown := r.queue.Get()
		if shutdown {
			return
		}
		if err := r.reconcile(ctx); err != nil && ctx.Err() == nil {
			slog.WarnContext(ctx, "reconcile failed; retrying", "error", err)
			r.queue.AddRateLimited(k)
		} else {
			r.queue.Forget(k)
		}
		r.queue.Done(k)
	}
}
