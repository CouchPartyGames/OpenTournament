// Command opentournament runs the Open Tournament API and its background
// workers.
package main

import (
	"context"
	"crypto/rand"
	"encoding/binary"
	"errors"
	"fmt"
	"log/slog"
	mrand "math/rand/v2"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/couchpartygames/opentournament/internal/app"
	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/clock"
	"github.com/couchpartygames/opentournament/internal/config"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/gameserver/agones"
	"github.com/couchpartygames/opentournament/internal/gameserver/gameservertest"
	"github.com/couchpartygames/opentournament/internal/telemetry"
	"github.com/exaring/otelpgx"
	"github.com/jackc/pgx/v5/pgxpool"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
)

// version is set at build time with -ldflags "-X main.version=…".
var version = "dev"

func main() {
	if err := run(); err != nil {
		slog.Error("fatal", "error", err)
		os.Exit(1)
	}
}

func run() error {
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGTERM, os.Interrupt)
	defer stop()

	cfg, err := config.FromEnv()
	if err != nil {
		return err
	}
	shutdownTelemetry, err := telemetry.Setup(ctx, version)
	if err != nil {
		return fmt.Errorf("telemetry: %w", err)
	}
	defer func() {
		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		if err := shutdownTelemetry(ctx); err != nil {
			slog.Warn("flush telemetry", "error", err)
		}
	}()

	catalog, err := games.Load(cfg.GamesFile)
	if err != nil {
		return err
	}
	poolCfg, err := pgxpool.ParseConfig(cfg.DatabaseURL)
	if err != nil {
		return fmt.Errorf("database url: %w", err)
	}
	poolCfg.ConnConfig.Tracer = otelpgx.NewTracer()
	pool, err := pgxpool.NewWithConfig(ctx, poolCfg)
	if err != nil {
		return err
	}
	defer pool.Close()
	if err := db.Migrate(ctx, pool); err != nil {
		return err
	}
	verifier, err := auth.NewOIDC(ctx, cfg.OIDCIssuer, catalog.IdentityClaims)
	if err != nil {
		return err
	}
	servers, err := gameServers(ctx, cfg, catalog)
	if err != nil {
		return err
	}

	var seed [16]byte
	rand.Read(seed[:]) // crypto/rand.Read never fails
	a, err := app.New(app.Config{
		Pool: pool, Clock: clock.Real{}, Games: catalog, Verifier: verifier,
		Rand:          mrand.New(mrand.NewPCG(binary.LittleEndian.Uint64(seed[:8]), binary.LittleEndian.Uint64(seed[8:]))),
		MatchTokenKey: cfg.MatchTokenKey, GameServers: servers, Docs: cfg.Docs, Version: version,
	})
	if err != nil {
		return err
	}
	a.Scheduler.Retention = cfg.EventRetention

	srv := &http.Server{
		Addr:              cfg.HTTPAddr,
		Handler:           otelhttp.NewHandler(a.Handler, "http", otelhttp.WithFilter(notProbe)),
		ReadHeaderTimeout: 10 * time.Second,
	}
	workersCtx, stopWorkers := context.WithCancel(context.WithoutCancel(ctx))
	workersDone := make(chan struct{})
	go func() {
		defer close(workersDone)
		a.Run(workersCtx)
	}()
	serveErr := make(chan error, 1)
	go func() {
		slog.Info("listening", "addr", cfg.HTTPAddr, "version", version, "game_servers", cfg.GameServers)
		serveErr <- srv.ListenAndServe()
	}()

	select {
	case err := <-serveErr:
		stopWorkers()
		<-workersDone
		return err
	case <-ctx.Done():
	}
	// Graceful shutdown: fail readiness, stop accepting requests and drain
	// the in-flight ones, then stop the background workers.
	slog.Info("shutting down")
	a.Drain()
	shutdownCtx, cancel := context.WithTimeout(context.Background(), cfg.ShutdownTimeout)
	defer cancel()
	err = srv.Shutdown(shutdownCtx)
	stopWorkers()
	select {
	case <-workersDone:
	case <-shutdownCtx.Done():
		err = errors.Join(err, errors.New("background workers did not stop in time"))
	}
	return err
}

func notProbe(r *http.Request) bool {
	switch r.URL.Path {
	case "/livez", "/readyz", "/startupz":
		return false
	}
	return true
}

func gameServers(ctx context.Context, cfg config.Config, catalog *games.Catalog) (gameserver.Port, error) {
	if cfg.GameServers == "fake" {
		slog.Warn("using the in-memory fake game server port; matches will not get real servers")
		return gameservertest.NewFake(), nil
	}
	var rc *rest.Config
	var err error
	if cfg.Kubeconfig != "" {
		rc, err = clientcmd.BuildConfigFromFlags("", cfg.Kubeconfig)
	} else {
		rc, err = rest.InClusterConfig()
	}
	if err != nil {
		return nil, fmt.Errorf("kubernetes config: %w", err)
	}
	return agones.New(ctx, rc, catalog.Namespaces())
}
