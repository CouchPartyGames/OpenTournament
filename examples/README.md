# Examples

- [`games.yaml`](games.yaml): a Game catalog.
- [`tournament.yaml`](tournament.yaml): a Tournament Manifest.
- [`recurring-tournament.yaml`](recurring-tournament.yaml): a Recurring Tournament.
- [`kubernetes/`](kubernetes): the values and manifests for a minimal cluster setup.

## Install on kind

A local [kind](https://kind.sigs.k8s.io) cluster with everything Open Tournament
needs: Agones with one Fleet, PostgreSQL through CloudNativePG, a development
Keycloak, and Open Tournament itself. It is for trying the service, not for
production: one replica of everything, Keycloak in dev mode with fixed
credentials, and no TLS.

You need `kind`, `kubectl`, `helm`, `curl` and `jq`, plus Docker or Podman.
Run the commands from this directory.

### 1. Cluster

```sh
kind create cluster --name ot
kubectl cluster-info --context kind-ot
```

### 2. Agones and a Fleet

Build the [C# reference Game Server](csharp-game-server/README.md) and load it into
kind. It plays each Match by simulation, so a whole Tournament runs without manual steps.

```sh
docker build -t opentournament-game-server:dev -f csharp-game-server/Dockerfile ..
kind load docker-image opentournament-game-server:dev --name ot
```

With Podman, `kind load docker-image` needs `KIND_EXPERIMENTAL_PROVIDER=podman`,
or save the image with `podman save` and use `kind load image-archive`.

```sh
helm install agones agones --repo https://agones.dev/chart/stable --version 1.61.0 \
  -n agones-system --create-namespace --wait
kubectl apply -f kubernetes/fleet.yaml
kubectl get fleet arena
```

[`fleet.yaml`](kubernetes/fleet.yaml) keeps two of these Game Servers warm in the
`default` namespace. Each reports Match started, plays Bouts for
`BOUT_DURATION_SECONDS` (5) each with random winners, then shuts itself down.

### 3. PostgreSQL

```sh
helm install cnpg cloudnative-pg --repo https://cloudnative-pg.github.io/charts --version 0.29.1 \
  -n cnpg-system --create-namespace --wait
kubectl create namespace opentournament
helm install postgres cluster --repo https://cloudnative-pg.github.io/charts --version 0.8.1 \
  -n opentournament -f kubernetes/postgres-values.yaml
```

CloudNativePG writes the connection string to the Secret `postgres-cluster-app`
(key `uri`), which Open Tournament reads.

### 4. Keycloak

Open Tournament verifies tokens from a Keycloak realm and will not start without
one. [`realm.json`](kubernetes/realm.json) creates the realm `games` with one
service client, `arena-backend`, the Game backend that creates Tournaments.

```sh
kubectl -n opentournament create configmap games-realm --from-file=kubernetes/realm.json
helm install keycloak keycloakx --repo https://codecentric.github.io/helm-charts --version 7.3.2 \
  -n opentournament -f kubernetes/keycloak-values.yaml
```

The issuer in [`keycloak-values.yaml`](kubernetes/keycloak-values.yaml) and
[`opentournament-values.yaml`](kubernetes/opentournament-values.yaml) is
`http://keycloak-http.opentournament`, so keep the namespace `opentournament`.

### 5. Open Tournament

Wait for the database and Keycloak, then install the released chart:

```sh
kubectl -n opentournament wait cluster.postgresql.cnpg.io/postgres-cluster --for=condition=Ready --timeout=5m
kubectl -n opentournament rollout status statefulset/keycloak --timeout=5m
helm install ot oci://ghcr.io/couchpartygames/charts/opentournament --version 0.0.4 \
  -n opentournament -f kubernetes/opentournament-values.yaml --wait
```

Or build the image from your checkout, load it into kind, and install the chart
from [`deploy/helm/opentournament`](../deploy/helm/opentournament):

```sh
make -C .. image VERSION=dev
kind load docker-image opentournament:dev --name ot
helm install ot ../deploy/helm/opentournament -n opentournament \
  -f kubernetes/opentournament-values.yaml \
  --set image.repository=opentournament --set image.tag=dev --wait
```

With Podman, `kind load docker-image` needs `KIND_EXPERIMENTAL_PROVIDER=podman`,
or save the image with `podman save` and use `kind load image-archive`.

To try the service without Game Servers, skip step 2 and add
`--set config.gameServers=fake`.

### Try it

```sh
kubectl -n opentournament port-forward svc/ot-opentournament 8080:80 &
kubectl -n opentournament port-forward svc/keycloak-http 8081:80 &

TOKEN=$(curl -s localhost:8081/realms/games/protocol/openid-connect/token \
  -d grant_type=client_credentials -d client_id=arena-backend -d client_secret=arena-backend-secret \
  | jq -r .access_token)

curl -s -X POST localhost:8080/api/v1/tournaments \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "{
    \"gameId\": \"arena\", \"name\": \"First Cup\", \"capacity\": 8, \"minimumParticipants\": 2,
    \"registrationOpensAt\": \"$(date -u +%FT%TZ)\",
    \"startsAt\": \"$(date -u -d '+10 min' +%FT%TZ)\",
    \"stages\": [{\"format\": \"single-elimination\", \"bestOf\": 1, \"resultDeadlineSeconds\": 600}]
  }"

curl -s localhost:8080/api/v1/tournaments | jq
```

`date -d` is GNU date; on macOS use `date -u -v+10M +%FT%TZ`.

The API docs are at <http://localhost:8080/api/v1/docs>, and the Keycloak admin
console at <http://localhost:8081> (`admin` / `admin`).

### Clean up

```sh
kind delete cluster --name ot
```

The [C# reference Game Server](csharp-game-server/README.md) is a .NET 10 single-file
application that plays an allocated head-to-head Match by simulation.
