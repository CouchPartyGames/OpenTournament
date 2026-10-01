# C# reference Game Server

A .NET 10 file-based application: [`GameServer.cs`](GameServer.cs) is the entry
point, with a project reference to `AgonesTournament.Sdk.GameServer` and the
simulation in [`MatchSimulation.cs`](MatchSimulation.cs) (added to the build by
[`Directory.Build.props`](Directory.Build.props) and covered by the SDK test suite).
It marks Agones Ready, sends health pings every two seconds, waits for Allocated,
reads the Match assignment through the SDK, and fetches and logs its roster.

It then plays a head-to-head Match by simulation, with no real players. It reports
Match started once, waits `BOUT_DURATION_SECONDS` per Bout, and reports a random
Participant as each Bout's winner in order from Bout 1. It stops after one
Participant wins the majority of the Best-of (one Bout for best-of-1, two for
best-of-3), then shuts down through the Agones SDK. A report that fails transiently
(network error, timeout, HTTP 5xx or 429) is retried up to five times; this is safe
because identical reports are a no-op. After an Abort it resumes from the Match's
completed Bouts. Free-for-all Matches are not simulated: the server logs that and
waits for shutdown.

The [Agones C# SDK](https://agones.dev/site/docs/guides/client-sdks/csharp/) uses
gRPC on localhost:9357 by default. `AGONES_SDK_GRPC_HOST` and
`AGONES_SDK_GRPC_PORT` override this. `OPENTOURNAMENT_API_URL` is required and must
be the HTTP(S) service root, not `/api/v1`. Deployment path prefixes are supported.
`BOUT_DURATION_SECONDS` (default 5, range 0 to 3600) is the simulated time per Bout.

From the repository root:

```sh
dotnet build examples/csharp-game-server/GameServer.cs -c Release
OPENTOURNAMENT_API_URL=http://localhost:8080 \
  dotnet run --file examples/csharp-game-server/GameServer.cs
```

The server rejects missing/invalid configuration before contacting Agones. A
permanent API rejection (such as 401), exhausted retries, and timeouts produce a
diagnostic and leave the allocated server running and healthy until SIGTERM or
Ctrl+C. It does not retry a rejected token. The Match token and response error bodies are never logged.
Agones failures stop the process with a nonzero exit code. Roster logs include
Player Identities as requested for this development example; restrict log access.

## Container and kind

Build from the repository root (Docker or Podman):

```sh
podman build -t docker.io/library/opentournament-game-server:dev \
  -f examples/csharp-game-server/Dockerfile .
kind load docker-image docker.io/library/opentournament-game-server:dev --name opentournament
```

For a kind cluster using Podman, export and load the image instead:

```sh
podman save -o /tmp/opentournament-game-server.tar docker.io/library/opentournament-game-server:dev
KIND_EXPERIMENTAL_PROVIDER=podman kind load image-archive \
  /tmp/opentournament-game-server.tar --name opentournament
```

Install the dependencies and Open Tournament using
[`../README.md`](../README.md). Its [`fleet.yaml`](../kubernetes/fleet.yaml) runs this
image as the Fleet `arena` in `default`, matching that setup's Game catalog, and sets
`OPENTOURNAMENT_API_URL` to the in-cluster service.

```sh
kubectl apply -f examples/kubernetes/fleet.yaml
kubectl get gameservers -n default -w
```

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
`Best-of=3`, two Participant lines, `Reported Match started.`, two or three
`Bout N: Participant <id> won.` lines, and `Match decided; shut down through Agones.`
The GameServer then disappears and the Fleet replaces it. Do not print its token
annotation. For a larger run, register 4 or more Participants with a best-of-3
single-elimination Stage and wait for Final Placements:
`curl -s localhost:8080/api/v1/tournaments/$TOURNAMENT_ID | jq`.

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
          image: opentournament-game-server:dev
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
