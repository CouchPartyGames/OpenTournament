package apptest

import (
	"context"
	"fmt"
	"net/url"
	"os"
	"strings"
	"sync"
	"sync/atomic"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/testcontainers/testcontainers-go"
	tcpostgres "github.com/testcontainers/testcontainers-go/modules/postgres"
	"github.com/testcontainers/testcontainers-go/wait"
)

// The tests share one PostgreSQL server. Migrations run once into a template
// database, and every test gets a fresh copy of it.
//
// Set OT_TEST_DATABASE_URL to an admin connection string to use an existing
// server instead of starting a container.

const template = "opentournament_template"

var (
	serverOnce sync.Once
	serverURL  string
	serverErr  error
	container  testcontainers.Container
	dbCounter  atomic.Int64
)

func startServer() {
	ctx := context.Background()
	if u := os.Getenv("OT_TEST_DATABASE_URL"); u != "" {
		serverURL = u
	} else {
		c, err := tcpostgres.Run(ctx, "docker.io/library/postgres:17-alpine",
			tcpostgres.WithDatabase("postgres"),
			tcpostgres.WithUsername("postgres"),
			tcpostgres.WithPassword("postgres"),
			testcontainers.WithWaitStrategy(wait.ForLog("database system is ready to accept connections").
				WithOccurrence(2).WithStartupTimeout(60*time.Second)),
		)
		if err != nil {
			serverErr = fmt.Errorf("start postgres container (is Docker or Podman running? see README): %w", err)
			return
		}
		container = c
		if serverURL, serverErr = c.ConnectionString(ctx, "sslmode=disable"); serverErr != nil {
			return
		}
	}
	admin, err := pgx.Connect(ctx, serverURL)
	if err != nil {
		serverErr = err
		return
	}
	defer admin.Close(ctx)
	if _, serverErr = admin.Exec(ctx, "DROP DATABASE IF EXISTS "+template); serverErr != nil {
		return
	}
	if _, serverErr = admin.Exec(ctx, "CREATE DATABASE "+template); serverErr != nil {
		return
	}
	pool, err := pgxpool.New(ctx, withDatabase(serverURL, template))
	if err != nil {
		serverErr = err
		return
	}
	defer pool.Close()
	serverErr = db.Migrate(ctx, pool)
}

func withDatabase(connString, name string) string {
	u, err := url.Parse(connString)
	if err != nil {
		panic(err)
	}
	u.Path = "/" + name
	return u.String()
}

// newDatabase creates an empty, migrated database for one test.
func newDatabase(t testing.TB) string {
	t.Helper()
	serverOnce.Do(startServer)
	if serverErr != nil {
		t.Fatal(serverErr)
	}
	ctx := context.Background()
	name := fmt.Sprintf("t_%d_%d_%s", os.Getpid(), dbCounter.Add(1), strings.ToLower(sanitize(t.Name())))
	if len(name) > 60 {
		name = name[:60]
	}
	admin, err := pgx.Connect(ctx, serverURL)
	if err != nil {
		t.Fatal(err)
	}
	defer admin.Close(ctx)
	if _, err := admin.Exec(ctx, fmt.Sprintf("CREATE DATABASE %s TEMPLATE %s", pgx.Identifier{name}.Sanitize(), template)); err != nil {
		t.Fatal(err)
	}
	return withDatabase(serverURL, name)
}

func sanitize(s string) string {
	var b strings.Builder
	for _, r := range s {
		if r >= 'a' && r <= 'z' || r >= 'A' && r <= 'Z' || r >= '0' && r <= '9' {
			b.WriteRune(r)
		} else {
			b.WriteRune('_')
		}
	}
	return b.String()
}

// Main runs a test binary's tests and stops the shared container afterwards.
// Use it from TestMain.
func Main(m *testing.M) {
	code := m.Run()
	if container != nil {
		_ = container.Terminate(context.Background())
	}
	os.Exit(code)
}
