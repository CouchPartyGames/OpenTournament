# Open Tournament

An MIT-licensed backend for real-time tournaments in online games, where players
register minutes before the start. It is the source of truth for Tournaments
**and** coordinates the [Agones](https://agones.dev) Game Servers the Matches are
played on: Game Servers report every Bout, and Standings, Advancement, later
Stages and Final Placements follow automatically.

- Vocabulary: [`CONTEXT.md`](CONTEXT.md)
- What v1 does: [`docs/specs/0001-realtime-tournaments-v1.md`](docs/specs/0001-realtime-tournaments-v1.md)
- Why it is built this way: [`docs/adr/`](docs/adr)

## Running it

The service needs PostgreSQL, a Keycloak realm, a Game catalog, and (for real
Game Servers) access to an Agones cluster. Configuration comes from the
environment:

| Variable | Meaning |
|---|---|
| `OT_DATABASE_URL` | PostgreSQL connection string. Migrations run at startup. |
| `OT_OIDC_ISSUER` | Keycloak realm URL, e.g. `https://sso.example.com/realms/games`. |
| `OT_GAMES_FILE` | The Game catalog, see [`examples/games.yaml`](examples/games.yaml). |
| `OT_MATCH_TOKEN_KEY` | Base64 key (≥ 32 bytes) signing Match tokens. Every replica needs the same key. |
| `OT_GAME_SERVERS` | `agones` (default), or `fake` for local development without a cluster. |
| `OT_KUBECONFIG` | Kubeconfig outside a cluster; in-cluster config is used when empty. |
| `OT_MANIFEST_NAMESPACES` | Comma-separated namespaces whose Tournament Manifests declare Tournaments, see [Declarative Tournaments](#declarative-tournaments). Empty (the default) turns them off. |
| `OT_HTTP_ADDR` | Listen address, default `:8080`. |
| `OT_DOCS` | `true` serves Scalar API docs at `/api/v1/docs`. |
| `OT_EVENT_RETENTION` | How long live-update events are kept, default `1h`. |
| `OT_SHUTDOWN_TIMEOUT` | Graceful shutdown bound, default `30s`. |
| `OTEL_*` | Standard OpenTelemetry settings, e.g. `OTEL_EXPORTER_OTLP_ENDPOINT`; `OTEL_SDK_DISABLED=true` logs to stdout only. |

```sh
make build
OT_DATABASE_URL=postgres://… OT_OIDC_ISSUER=https://… OT_GAMES_FILE=examples/games.yaml \
OT_MATCH_TOKEN_KEY=$(head -c 32 /dev/urandom | base64) OT_GAME_SERVERS=fake OT_DOCS=true \
./bin/opentournament
```

CI publishes images to `ghcr.io/couchpartygames/opentournament`: `:main` and
`:sha-<commit>` for every commit to `main`, and `:<version>`, `:<major>.<minor>`
and `:latest` for every `v*` tag.

The API lives under `/api/v1`; its OpenAPI 3.1 document is at `/api/v1/openapi.json`.
Health checks are `/livez`, `/readyz` and `/startupz`. On `SIGTERM` the service
fails readiness, drains in-flight requests, then stops its background workers.

Several replicas can run side by side: scheduled work is claimed with
`SELECT … FOR UPDATE SKIP LOCKED`, Match state changes are conditional updates,
and live updates fan out through PostgreSQL `LISTEN`/`NOTIFY`.

### Kubernetes

The Helm chart in [`deploy/helm/opentournament`](deploy/helm/opentournament) deploys
the service, its Game catalog and secrets, and a Role in every Fleet namespace the
catalog names. PostgreSQL and Keycloak are not part of it.

```sh
helm install ot oci://ghcr.io/couchpartygames/charts/opentournament --version <version> \
  -n opentournament --create-namespace -f my-values.yaml
```

See the [chart README](deploy/helm/opentournament/README.md) for its values, and
[`examples/kubernetes`](examples/kubernetes) for a minimal setup of everything it
needs. Creating Fleets and sizing them for the start-of-Tournament burst is an
operations concern: every first-Round Match wants a Game Server at the same moment.

### Declarative Tournaments

Tournaments can also live in git as Tournament Manifests, synced into the cluster
by Argo CD or Flux ([ADR-0005](docs/adr/0005-tournament-manifests-declare-configuration-in-kubernetes.md)).
A Manifest holds only configuration: the same settings `POST /api/v1/tournaments`
takes, plus the Organizer. Registrations, Matches and every other runtime state
stay in PostgreSQL.

1. Install the `Tournament` CRD. The chart installs it from its `crds/` directory;
   without the chart, apply
   [`tournaments.opentournament.io.yaml`](deploy/helm/opentournament/crds/tournaments.opentournament.io.yaml).
   Helm doesn't upgrade CRDs, so apply that file again after upgrading.
2. List the namespaces to watch in `config.manifestNamespaces` (or
   `OT_MANIFEST_NAMESPACES`). The chart grants the RBAC to read Manifests and update
   their status in each of them.
3. Apply a Manifest, such as [`examples/tournament.yaml`](examples/tournament.yaml),
   in a watched namespace.

The service creates a Draft Tournament organized by the Manifest's `organizer`
(`user:<keycloak subject>` or `client:<keycloak client id>`; a Game backend's client
must be trusted by the Game). The Tournament then shows up in
`GET /api/v1/tournaments` like any other, and the Manifest's status reports it:

```console
$ kubectl -n games get tournaments
NAME         STATUS   STARTS                 TOURNAMENT ID                          AGE
friday-cup   draft    2026-10-02T19:00:00Z   01a0e9a6-1733-7ad2-8031-ac9678bf08d4   5s
```

The status carries `observedGeneration`, `tournamentId`, `tournamentStatus` and a
`Synced` condition. Every replica watches the Manifests, without leader election;
a Manifest declares exactly one Tournament however often, or however many
replicas at once, reconcile it. The status follows the Tournament's own status
every 30 seconds.

Only creation is supported so far: editing a Manifest, reporting an invalid one
in its status, deleting it and recurring schedules are still to come. Until then,
an invalid Manifest is retried with backoff and logged as a warning.

## How it fits together

```
cmd/opentournament         wiring, configuration, graceful shutdown
internal/format            the Format engine: pure, no I/O (Seam 2)
internal/tournament        the Tournament aggregate and Match lifecycle
internal/lifecycle         the typed statuses shared by the aggregate and the queries
internal/features/*        vertical slices, one per feature, each registering its Huma operations
                           (manifests watches Tournament Manifests instead)
internal/scheduler         persisted due times: registration, check-in, start, allocation, Result Deadlines
internal/reconciler        level-triggered reconcile loop between Matches and GameServers
internal/gameserver        the Game Server port, its Agones adapter and an in-memory fake
internal/events            the live-update outbox (ADR-0003)
internal/db                migrations and sqlc-generated queries
```

### Game Servers

When a Match becomes Ready the service allocates a GameServer from the Game's
Fleet. The allocation carries these labels and annotations, which the Game
Server reads through the Agones SDK:

| Key | Meaning |
|---|---|
| `opentournament/match-id` (label) | The Match to host. |
| `opentournament/match-token` (annotation) | The Match token. Send it as `Authorization: Bearer <token>` to `/api/v1/game-server/*`. |
| `opentournament/forfeited` (annotation, updated live) | Comma-separated Participant IDs who withdrew or were disqualified: kick them. |

The Game Server fetches its Match (`GET /api/v1/game-server/match`), reports it
started, then reports each Bout (`PUT /api/v1/game-server/match/bouts/{n}`) and any
No-shows. The token only works for that Match, on that allocation, until the Match
ends or its Result Deadline (plus a short grace) passes. After an Abort the
replacement server gets a new token and resumes after the Bouts already completed.

### Live updates

Open one WebSocket to `/api/v1/live`. Optionally send
`{"type":"authenticate","token":"<keycloak token>"}` first, then
`{"type":"subscribe","tournamentId":"…"}` for any number of Tournaments. The
`subscribed` reply carries the last sequence number already sent; fetch state over
REST, then apply events with higher numbers. A gap in sequence numbers means
refetch. The Game Server address of a Match is only sent to its Participants and
the Organizer.

## Developing

Tests run against a real PostgreSQL through testcontainers (Docker or Podman), or
against an existing server named by `OT_TEST_DATABASE_URL` (an admin connection
string; each test gets its own database).

```sh
make test          # everything; with Podman: systemctl --user start podman.socket first
make test-engine   # just the Format engine, no database needed
make generate      # regenerate internal/db from the SQL in internal/db/queries (needs sqlc)
```

Tests sit at two seams. The HTTP API tests host the real app in-process with a
fake clock, a fake Game Server port, a seeded random source and a test JWT issuer
in place of Keycloak, and read like Tournament scenarios. The Format engine has
its own unit and property tests.

If a test run is killed before it finishes, its PostgreSQL container can be left
behind: `docker rm -f $(docker ps -aq --filter label=org.testcontainers=true)`.

### Manual smoke test against Agones

The Agones adapter has no automated tests in v1. To try it on a local cluster:

1. Create a cluster and install Agones:
   ```sh
   kind create cluster --name ot
   helm repo add agones https://agones.dev/chart/stable
   helm install agones agones/agones --namespace agones-system --create-namespace
   ```
2. Create the `games` namespace and a Fleet named `arena` in it, e.g. from the
   Agones [simple-game-server example](https://agones.dev/site/docs/getting-started/create-fleet/)
   with `metadata.name: arena` and a few replicas.
3. Install the [chart](deploy/helm/opentournament) with a catalog whose `arena` Game
   points at that Fleet, or run the service locally with `OT_GAME_SERVERS=agones` and
   your own kubeconfig via `OT_KUBECONFIG`.
4. Create a Tournament with two Participants that starts in a minute, and wait.
   Check:
   - `kubectl -n games get gs` shows an `Allocated` GameServer labelled
     `opentournament/match-id=<match>`, carrying the `opentournament/match-token` annotation;
   - `GET /api/v1/matches/{id}` as a Participant shows the server's address and port;
   - with that token, `curl -H "Authorization: Bearer $TOKEN" …/api/v1/game-server/match` returns the Match;
   - `kubectl -n games delete gs <name>` makes the Match Abort and get a new server within seconds;
   - withdrawing a Participant of a running free-for-all Match sets `opentournament/forfeited`;
   - reporting the final Bout deletes the GameServer.

## License

MIT, see [`LICENSE`](LICENSE).
