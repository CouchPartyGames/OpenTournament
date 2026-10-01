// Package app composes the service: the HTTP API with every feature slice,
// and the background workers.
package app

import (
	"context"
	"log/slog"
	"math/rand/v2"
	"net/http"
	"sync"
	"sync/atomic"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/clock"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/features/gameservers"
	"github.com/couchpartygames/opentournament/internal/features/live"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/matchtoken"
	"github.com/couchpartygames/opentournament/internal/reconciler"
	"github.com/couchpartygames/opentournament/internal/scheduler"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/jackc/pgx/v5/pgxpool"
	"k8s.io/client-go/dynamic"
)

// Config holds the service's dependencies.
type Config struct {
	Pool          *pgxpool.Pool
	Clock         clock.Clock
	Rand          *rand.Rand
	Games         *games.Catalog
	Verifier      auth.Verifier
	MatchTokenKey []byte
	GameServers   gameserver.Port
	// Kubernetes is where Tournament Manifests are read from. It is only used
	// when ManifestNamespaces names at least one namespace.
	Kubernetes dynamic.Interface
	// ManifestNamespaces are the namespaces whose Tournament Manifests and
	// Recurring Tournaments are declared. None turns declared Tournaments off.
	ManifestNamespaces []string
	// ManifestLogger receives Manifest controller diagnostics. It must be
	// non-nil when ManifestNamespaces is non-empty.
	ManifestLogger *slog.Logger
	// Docs serves interactive API docs (Scalar), for development.
	Docs    bool
	Version string
}

// App is the composed service.
type App struct {
	Handler    http.Handler
	API        huma.API
	Service    *tournament.Service
	Scheduler  *scheduler.Scheduler
	Reconciler *reconciler.Runner
	Hub        *live.Hub
	// Manifests is nil when no namespace is watched.
	Manifests *manifests.Controller

	pool     *pgxpool.Pool
	started  atomic.Bool
	draining atomic.Bool
}

// APIPrefix is where the versioned API lives.
const APIPrefix = "/api/v1"

// New composes the service.
func New(cfg Config) (*App, error) {
	tokens, err := matchtoken.NewIssuer(cfg.MatchTokenKey, cfg.Clock)
	if err != nil {
		return nil, err
	}
	svc := tournament.NewService(tournament.Deps{
		Pool: cfg.Pool, Clock: cfg.Clock, Games: cfg.Games, Tokens: tokens, Servers: cfg.GameServers, Rand: cfg.Rand,
	})
	sched := scheduler.New(db.New(cfg.Pool), cfg.Clock, map[lifecycle.JobKind]scheduler.Handler{
		lifecycle.JobOpenRegistration: func(ctx context.Context, j db.Job) error { return svc.OpenRegistration(ctx, j.TournamentID) },
		lifecycle.JobOpenCheckIn:      func(ctx context.Context, j db.Job) error { return svc.OpenCheckIn(ctx, j.TournamentID) },
		lifecycle.JobStart:            func(ctx context.Context, j db.Job) error { return svc.Start(ctx, j.TournamentID) },
		lifecycle.JobAllocate:         func(ctx context.Context, j db.Job) error { return svc.Allocate(ctx, j.MatchID) },
		lifecycle.JobResultDeadline:   func(ctx context.Context, j db.Job) error { return svc.ExpireMatch(ctx, j.MatchID) },
	})
	svc.Wake = sched.Wake

	a := &App{
		Service:    svc,
		Scheduler:  sched,
		Reconciler: reconciler.New(svc.Reconcile, cfg.GameServers.Watch),
		Hub:        live.NewHub(cfg.Pool, cfg.Verifier),
		pool:       cfg.Pool,
	}
	if len(cfg.ManifestNamespaces) > 0 {
		a.Manifests = manifests.New(cfg.Kubernetes, svc, cfg.ManifestNamespaces, cfg.ManifestLogger)
	}

	mux := http.NewServeMux()
	a.API = newAPI(mux, svc, cfg)
	mux.Handle("GET "+APIPrefix+"/live", a.Hub)

	mux.HandleFunc("GET /livez", func(w http.ResponseWriter, _ *http.Request) { w.WriteHeader(http.StatusOK) })
	mux.HandleFunc("GET /startupz", func(w http.ResponseWriter, _ *http.Request) {
		if !a.started.Load() {
			w.WriteHeader(http.StatusServiceUnavailable)
			return
		}
		w.WriteHeader(http.StatusOK)
	})
	mux.HandleFunc("GET /readyz", func(w http.ResponseWriter, r *http.Request) {
		ctx, cancel := context.WithTimeout(r.Context(), 2*time.Second)
		defer cancel()
		if a.draining.Load() || !a.started.Load() || a.pool.Ping(ctx) != nil {
			w.WriteHeader(http.StatusServiceUnavailable)
			return
		}
		w.WriteHeader(http.StatusOK)
	})

	a.Handler = auth.Middleware(cfg.Verifier, gameservers.PathPrefix)(mux)
	return a, nil
}

// Run starts the background workers and blocks until ctx ends and they
// have stopped.
func (a *App) Run(ctx context.Context) {
	var wg sync.WaitGroup
	workers := []func(context.Context){a.Scheduler.Run, a.Reconciler.Run, a.Hub.Run}
	if a.Manifests != nil {
		workers = append(workers, a.Manifests.Run)
	}
	for _, run := range workers {
		wg.Add(1)
		go func() {
			defer wg.Done()
			run(ctx)
		}()
	}
	a.started.Store(true)
	wg.Wait()
}

// Drain makes the readiness check fail, so traffic moves elsewhere before
// shutdown.
func (a *App) Drain() { a.draining.Store(true) }
