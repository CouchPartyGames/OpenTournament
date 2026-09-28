// Package app composes the service: the HTTP API with every feature slice,
// and the background workers.
package app

import (
	"context"
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
	"github.com/couchpartygames/opentournament/internal/features/matches"
	"github.com/couchpartygames/opentournament/internal/features/participants"
	"github.com/couchpartygames/opentournament/internal/features/registrations"
	"github.com/couchpartygames/opentournament/internal/features/structure"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/matchtoken"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/reconciler"
	"github.com/couchpartygames/opentournament/internal/scheduler"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/danielgtaylor/huma/v2/adapters/humago"
	"github.com/jackc/pgx/v5/pgxpool"
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

	pool     *pgxpool.Pool
	started  atomic.Bool
	draining atomic.Bool
}

// APIPrefix is where the versioned API lives.
const APIPrefix = "/api/v1"

// New composes the service.
func New(cfg Config) (*App, error) {
	problem.Install()
	tokens, err := matchtoken.NewIssuer(cfg.MatchTokenKey, cfg.Clock)
	if err != nil {
		return nil, err
	}
	svc := tournament.NewService(tournament.Deps{
		Pool: cfg.Pool, Clock: cfg.Clock, Games: cfg.Games, Tokens: tokens, Servers: cfg.GameServers, Rand: cfg.Rand,
	})
	sched := scheduler.New(db.New(cfg.Pool), cfg.Clock, map[string]scheduler.Handler{
		tournament.JobOpenRegistration: func(ctx context.Context, j db.Job) error { return svc.OpenRegistration(ctx, j.TournamentID) },
		tournament.JobOpenCheckIn:      func(ctx context.Context, j db.Job) error { return svc.OpenCheckIn(ctx, j.TournamentID) },
		tournament.JobStart:            func(ctx context.Context, j db.Job) error { return svc.Start(ctx, j.TournamentID) },
		tournament.JobAllocate:         func(ctx context.Context, j db.Job) error { return svc.Allocate(ctx, j.MatchID) },
		tournament.JobResultDeadline:   func(ctx context.Context, j db.Job) error { return svc.ExpireMatch(ctx, j.MatchID) },
	})
	svc.Wake = sched.Wake

	a := &App{
		Service:    svc,
		Scheduler:  sched,
		Reconciler: reconciler.New(svc.Reconcile, cfg.GameServers.Watch),
		Hub:        live.NewHub(cfg.Pool, cfg.Verifier),
		pool:       cfg.Pool,
	}

	mux := http.NewServeMux()
	hc := huma.DefaultConfig("Open Tournament API", cfg.Version)
	hc.Info.Description = "Real-time tournaments for online games, played on Agones Game Servers."
	hc.OpenAPIPath = APIPrefix + "/openapi"
	hc.DocsPath = ""
	if cfg.Docs {
		hc.DocsPath = APIPrefix + "/docs"
		hc.DocsRenderer = huma.DocsRendererScalar
	}
	hc.SchemasPath = APIPrefix + "/schemas"
	hc.Components.SecuritySchemes = map[string]*huma.SecurityScheme{
		"keycloak":   {Type: "http", Scheme: "bearer", BearerFormat: "JWT", Description: "A Keycloak-issued access token."},
		"matchToken": {Type: "http", Scheme: "bearer", BearerFormat: "JWT", Description: "The per-Match token a Game Server receives when it is allocated."},
	}
	api := humago.New(mux, hc)
	a.API = api

	tournaments.Register(api, svc)
	registrations.Register(api, svc)
	participants.Register(api, svc)
	structure.Register(api, svc)
	matches.Register(api, svc)
	gameservers.Register(api, svc)
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
	for _, run := range []func(context.Context){a.Scheduler.Run, a.Reconciler.Run, a.Hub.Run} {
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
