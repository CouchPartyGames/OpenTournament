# Minimal Kubernetes setup

Everything Open Tournament needs, installed with Helm on any cluster (kind, k3s,
minikube): Agones with one Fleet, PostgreSQL through CloudNativePG, a development
Keycloak, and Open Tournament itself. It is for trying the service, not for
production: one replica of everything, Keycloak in dev mode with fixed
credentials, and no TLS.

Run the commands from this directory, or run [`./install.sh`](install.sh) to do
steps 1 to 4 in one go (`OPENTOURNAMENT_VERSION` picks the chart version).

## 1. Agones and a Fleet

```sh
helm install agones agones --repo https://agones.dev/chart/stable --version 1.61.0 \
  -n agones-system --create-namespace --wait
kubectl apply -f fleet.yaml
```

[`fleet.yaml`](fleet.yaml) keeps two `simple-game-server`s warm in the `default`
namespace, the one Agones manages out of the box.

## 2. PostgreSQL

```sh
helm install cnpg cloudnative-pg --repo https://cloudnative-pg.github.io/charts --version 0.29.1 \
  -n cnpg-system --create-namespace --wait
kubectl create namespace opentournament
helm install postgres cluster --repo https://cloudnative-pg.github.io/charts --version 0.8.1 \
  -n opentournament -f postgres-values.yaml
```

CloudNativePG writes the connection string to the Secret `postgres-cluster-app`
(key `uri`), which Open Tournament reads.

## 3. Keycloak

Open Tournament verifies tokens from a Keycloak realm and will not start without
one. [`realm.json`](realm.json) creates the realm `games` with one service client,
`arena-backend`, the Game backend that creates Tournaments.

```sh
kubectl -n opentournament create configmap games-realm --from-file=realm.json
helm install keycloak keycloakx --repo https://codecentric.github.io/helm-charts --version 7.3.2 \
  -n opentournament -f keycloak-values.yaml
```

## 4. Open Tournament

Wait for the database and Keycloak, then install the chart:

```sh
kubectl -n opentournament wait cluster.postgresql.cnpg.io/postgres-cluster --for=condition=Ready --timeout=5m
kubectl -n opentournament rollout status statefulset/keycloak --timeout=5m
helm install ot oci://ghcr.io/couchpartygames/charts/opentournament --version 0.0.4 \
  -n opentournament -f opentournament-values.yaml --wait
```

[`opentournament-values.yaml`](opentournament-values.yaml) has the one Game, `arena`,
played on the Fleet from step 1.

## Try it

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

The API docs are at <http://localhost:8080/api/v1/docs>, and the Keycloak admin
console at <http://localhost:8081> (`admin` / `admin`).

`date -d` is GNU date; on macOS use `date -u -v+10M +%FT%TZ`.

## Clean up

```sh
helm uninstall ot keycloak postgres -n opentournament
kubectl delete namespace opentournament
kubectl delete -f fleet.yaml
helm uninstall cnpg -n cnpg-system
helm uninstall agones -n agones-system
```

Helm leaves the CloudNativePG and Agones CRDs behind; delete them with
`kubectl delete crd` if you want the cluster back as it was.
