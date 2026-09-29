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
| `OT_MANIFEST_NAMESPACES` | Comma-separated namespaces whose Tournament Manifests and Recurring Tournaments declare Tournaments, see [Declarative Tournaments](#declarative-tournaments). Empty (the default) turns them off. |
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

1. Install the `Tournament` and `RecurringTournament` CRDs. The chart installs them
   from its `crds/` directory; without the chart, apply
   [`deploy/helm/opentournament/crds`](deploy/helm/opentournament/crds).
   Helm neither upgrades CRDs nor adds new ones, so apply that directory again
   after upgrading. Without the `RecurringTournament` CRD, Tournament Manifests
   still work, and client-go logs that it fails to watch Recurring Tournaments.
2. List the namespaces to watch in `config.manifestNamespaces` (or
   `OT_MANIFEST_NAMESPACES`). The chart grants the RBAC to read Manifests and
   Recurring Tournaments and update their status in each of them.
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

Git is the source of truth for a declared Tournament's settings. Editing its
Manifest while the Tournament is a Draft edits the Tournament, with the same
validation as `PUT /api/v1/tournaments/{id}`, and the status reports
`Synced=True` with reason `Updated` and the new `observedGeneration`. When the
Manifest can't be applied, nothing changes, `observedGeneration` stays the last
generation applied, and `Synced=False` says why:

| Reason | When |
|---|---|
| `SettingsFrozen` | The Manifest changed after registration opened, when settings freeze. Reverting the change syncs it again. |
| `ValidationFailed` | The settings break a rule the API enforces, or the Manifest changes its `gameId` or `organizer`. The message lists every field. |
| `OrganizerNotTrusted` | The `client:` Organizer isn't trusted by the Game. |

`kubectl -n games get tournaments -o wide` shows the `Synced` condition and its
reason. The API refuses to edit a declared Tournament with `409 declared-in-git`.
Cancelling it through the API still works as an emergency lever: the Tournament
stays cancelled, and its Manifest keeps pointing to it.

Deleting a Manifest deletes its Draft Tournament, or cancels it if Registration
or Check-in has opened, using the same cancellation path as the API (including
Game Server release and live status updates). Running, Completed and Cancelled
Tournaments are left untouched.

A removal sweep runs at startup after every watch has synced, then every 30
seconds, so deletions made while the service was down are handled too. Only
Tournaments declared in currently watched namespaces are considered; removing a
namespace from the watched list leaves its Tournaments alone. If a sweep would
delete or cancel more than half of those declared Tournaments, Occurrences of
Recurring Tournaments included, it logs an error with the counts and skips all
removals. This also protects a namespace set with
only one declared Tournament. Restore the missing Manifests to bring the removal
count within the limit before retrying; normal sweeps retry automatically. No
Kubernetes finalizers are used.

### Recurring Tournaments

A `RecurringTournament` declares a Tournament for every **Occurrence** of a
schedule, from a template, such as
[`examples/recurring-tournament.yaml`](examples/recurring-tournament.yaml):

```yaml
spec:
  schedule: "0 20 * * 1-5"         # when each Tournament starts: five-field cron
  timeZone: Europe/Berlin          # an IANA time zone
  lookahead: 48h                   # declare Tournaments starting within this window (default 24h)
  suspend: false                   # stop declaring Tournaments, and remove those not yet started
  template:                        # the Tournament settings, minus the two absolute times
    organizer: client:arena-backend
    gameId: arena
    name: Weeknight Cup            # each Tournament: "Weeknight Cup 2026-10-02 20:00", in the time zone
    registrationOpensBefore: 30m   # replaces startsAt / registrationOpensAt
    capacity: 32
    # … minimumParticipants, checkIn and stages, as in a Tournament Manifest
```

The service declares a Draft Tournament for each Occurrence that starts within
the lookahead window, with registration opening `registrationOpensBefore` earlier.
As time passes it looks again when the next Occurrence enters the window, without
any change to the Recurring Tournament. Each Tournament records the Occurrence it
came from (`recurring/<namespace>/<name>/<start>`), so each Occurrence is declared
exactly once however many replicas reconcile it. Daylight-saving changes never
drop or repeat an Occurrence. Durations such as `lookahead` are Go durations
(`30m`, `48h`), which have no unit for days. Keep `lookahead` longer than
`registrationOpensBefore`, or registration opens as soon as each Tournament is
declared.

The status lists the upcoming Occurrences, the latest declared start and a `Synced`
condition:

```console
$ kubectl -n games get recurringtournaments
NAME            SCHEDULE       TIME ZONE       SUSPEND   LAST SCHEDULE   SYNCED   AGE
weeknight-cup   0 20 * * 1-5   Europe/Berlin   false     2d              True     5s
$ kubectl -n games get recurringtournament weeknight-cup -o jsonpath='{.status.upcoming}'
[{"startsAt":"2026-10-01T18:00:00Z","tournamentId":"01a0e9a6-…","tournamentStatus":"draft"}, …]
```

`Synced=True` has reason `Scheduled`, or `Suspended` while `suspend` is true. A
Recurring Tournament that can't be applied declares nothing, and `Synced=False`
says why:

| Reason | When |
|---|---|
| `InvalidSchedule` | The schedule isn't a five-field cron expression or descriptor such as `@daily`. `@every` and a `TZ=` prefix aren't supported. |
| `UnknownTimeZone` | The time zone isn't an IANA name such as `UTC` or `Europe/Berlin`. |
| `InvalidLookahead` | The lookahead isn't a duration, or is negative. |
| `ValidationFailed` | The template breaks a rule the API enforces. The message lists every field. |
| `OrganizerNotTrusted` | The `client:` Organizer isn't trusted by the Game. |

Git stays the source of truth for every Occurrence, as for a Tournament Manifest:

- **Editing the template** updates the Occurrences that are still Drafts. Those
  whose registration has opened keep their frozen settings: `Synced=False` with
  reason `SettingsFrozen` names them, while every other Occurrence is still
  declared and updated. The condition clears once they start.
- **Editing the schedule**, time zone or lookahead, or setting `suspend: true`,
  removes the Tournaments of Occurrences no longer wanted, and declares the new
  ones unless suspended. A Draft is deleted; one in Registration Open or Check-in
  is cancelled; a Running, Completed or Cancelled one is left alone. Only
  Occurrences still ahead are ever unwanted, so a schedule change never touches
  one that has started.
- **Deleting a Recurring Tournament** removes the Tournaments of all its
  Occurrences the same way, through the removal sweep and its guards. Occurrences
  are swept only once the Recurring Tournament watches have synced, so in a
  cluster without their CRD the sweep leaves them alone.

A Recurring Tournament that can't be applied changes none of its Occurrences.
The API refuses to edit a declared Occurrence with `409 declared-in-git`, and
can still cancel it; a cancelled Occurrence stays cancelled.

## How it fits together

```
cmd/opentournament         wiring, configuration, graceful shutdown
internal/format            the Format engine: pure, no I/O (Seam 2)
internal/tournament        the Tournament aggregate and Match lifecycle
internal/lifecycle         the typed statuses shared by the aggregate and the queries
internal/features/*        vertical slices, one per feature, each registering its Huma operations
                           (manifests watches Tournament Manifests and Recurring Tournaments instead)
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
5. Check that the image reads schedules in a time zone, though it has no time
   zone database of its own: install the chart with `config.manifestNamespaces: [tournaments]`,
   create the `tournaments` namespace, and apply
   [`examples/recurring-tournament.yaml`](examples/recurring-tournament.yaml) with
   `timeZone: Asia/Kolkata` (UTC+05:30, no daylight saving) and a `schedule` a few
   hours ahead. Check:
   - `kubectl -n tournaments get recurringtournaments` shows `Synced` `True`;
   - each Tournament in `GET /api/v1/tournaments` starts at the scheduled Kolkata
     time, e.g. 20:00 there is `14:30:00Z`, and its name shows 20:00.

## License

MIT, see [`LICENSE`](LICENSE).
