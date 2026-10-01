# C# reference Game Server

A .NET 10 file-based application: [`GameServer.cs`](GameServer.cs) is the entire
server, with a project reference to `AgonesTournament.Sdk.GameServer`. No server
`.csproj` is needed. It marks Agones Ready, sends health pings every two seconds,
waits for Allocated, reads the Match assignment through the SDK, and fetches and
logs its roster. It does not accept connections, start the Match, or report Bouts.
An allocated Match will eventually become Stalled unless an Organizer resolves it.

The [Agones C# SDK](https://agones.dev/site/docs/guides/client-sdks/csharp/) uses
gRPC on localhost:9357 by default. `AGONES_SDK_GRPC_HOST` and
`AGONES_SDK_GRPC_PORT` override this. `OPENTOURNAMENT_API_URL` is required and must
be the HTTP(S) service root, not `/api/v1`. Deployment path prefixes are supported.

From the repository root:

```sh
dotnet build examples/csharp-game-server/GameServer.cs -c Release
OPENTOURNAMENT_API_URL=http://localhost:8080 \
  dotnet run --file examples/csharp-game-server/GameServer.cs
```

The server rejects missing/invalid configuration before contacting Agones. API
401, HTTP failures, network failures, and timeouts produce a diagnostic and leave
the allocated server running and healthy until SIGTERM or Ctrl+C. It does not
retry a rejected token. The Match token and response error bodies are never logged.
Agones failures stop the process with a nonzero exit code. Roster logs include
Player Identities as requested for this development example; restrict log access.

## Container and kind

Build from the repository root (Docker or Podman):

```sh
podman build -t docker.io/library/opentournament-game-server:issue24 \
  -f examples/csharp-game-server/Dockerfile .
kind load docker-image docker.io/library/opentournament-game-server:issue24 --name opentournament
```

For a kind cluster using Podman, export and load the image instead:

```sh
podman save -o /tmp/opentournament-game-server.tar docker.io/library/opentournament-game-server:issue24
KIND_EXPERIMENTAL_PROVIDER=podman kind load image-archive \
  /tmp/opentournament-game-server.tar --name opentournament
```

Install the dependencies and Open Tournament using
[`../kubernetes/README.md`](../kubernetes/README.md). Replace the example Fleet
with this one after loading the image:

```sh
kubectl apply -f examples/csharp-game-server/fleet.yaml
kubectl get gameservers -n default -w
```

The Fleet is named `arena` in `default`, matching that setup's Game catalog. It
sets `OPENTOURNAMENT_API_URL` to the in-cluster service. Existing Ready servers
from the previous Fleet template may remain until replaced; for a fresh local
setup install this manifest instead of `examples/kubernetes/fleet.yaml`.

Use the API/Keycloak port forwards and backend token from that guide. Create a
Tournament starting shortly in the future, with Check-in disabled, and register
two Participants before its start using the trusted `arena-backend` token:

```sh
curl -fsS -X POST localhost:8080/api/v1/tournaments \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "{
    \"gameId\": \"arena\", \"name\": \"Roster smoke test\", \"capacity\": 2,
    \"minimumParticipants\": 2,
    \"registrationOpensAt\": \"$(date -u +%FT%TZ)\",
    \"startsAt\": \"$(date -u -d '+2 min' +%FT%TZ)\",
    \"stages\": [{\"format\": \"single-elimination\", \"bestOf\": 3,
                 \"resultDeadlineSeconds\": 600}]
  }" > /tmp/roster-tournament.json
TOURNAMENT_ID=$(jq -r .id /tmp/roster-tournament.json)
for identity in roster-one roster-two; do
  curl -fsS -X POST "localhost:8080/api/v1/tournaments/$TOURNAMENT_ID/participants" \
    -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "{\"identity\": {\"kind\": \"keycloak\", \"value\": \"$identity\"}}"
done
kubectl get gameservers -n default -w
```

After the Tournament starts, inspect the allocated GameServer's game container:

```sh
kubectl logs -n default <allocated-gameserver-name> -c game-server
```

Expect `Agones Ready`, `Allocated to Match <id>`, `Format=single-elimination`,
`Best-of=3`, and two Participant lines with `roster-one` / `roster-two` and
`forfeited=false`. Compare the Match ID with the Tournament structure and the
GameServer's `opentournament/match-id` label. Do not print its token annotation.

## Local Agones SDK server

Use the [official SDK server in local mode](https://agones.dev/site/docs/guides/client-sdks/local/)
(version 1.61.0, matching the C# dependency). Start it with a GameServer YAML file:

```sh
sdk-server.linux.amd64 --local --file /tmp/local-gameserver.yaml
```

A minimal local fixture is:

```yaml
apiVersion: agones.dev/v1
kind: GameServer
metadata:
  name: local-reference
  labels:
    opentournament/match-id: <allocated-match-uuid>
  annotations:
    opentournament/match-token: <valid-match-token>
spec:
  container: game-server
  template:
    spec:
      containers:
        - name: game-server
          image: opentournament-game-server:issue24
status:
  state: Allocated
```

Use credentials from a real, current allocation in your development deployment;
a made-up token should produce the clear 401 diagnostic. Keep the YAML private.
Run the application with the API URL above. The local SDK watches the file for
changes: to exercise waiting, start with `status.state: Ready` and no allocation
metadata, then write the complete Allocated snapshot after the app reports Ready.
Local mode updates state after Ready. With the credentials already in the fixture,
allocate it after that log using the local HTTP gateway:

```sh
curl -fsS -X POST http://localhost:9358/allocate \
  -H 'Content-Type: application/json' -d '{}'
```

Alternatively set Allocated in the watched file after the Ready log. Save changes
in place so the SDK's file watch remains attached to the same file. This simulates Agones
only; the API and its Match token validation remain real.

## Verification of this slice

Verified on an isolated kind cluster with Agones 1.61.0, the local Open Tournament
chart, PostgreSQL, and Keycloak. A two-Participant Tournament started and allocated
Match `01a0f8be-b467-7889-9bb3-b55477716b0d`; the container logged
`Format=single-elimination, Best-of=3` and the registered `roster-one` and
`roster-two` identities with `forfeited=false`. Both GameServers remained healthy.
The .NET build, container build, existing C# SDK suite (100 tests), and full Go
suite/vet passed. 