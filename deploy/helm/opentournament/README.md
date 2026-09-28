# Open Tournament Helm chart

Deploys [Open Tournament](../../../README.md): the service, its Game catalog, its
secrets, and a Role and RoleBinding in every Fleet namespace the catalog names.
Bring your own PostgreSQL, Keycloak realm and Agones Fleets.

It also installs the `Tournament` CRD for [declarative Tournaments](../../../README.md#declarative-tournaments)
from [`crds/`](crds). Helm installs CRDs only on the first install and never
upgrades or deletes them; after an upgrade, `kubectl apply -f crds/` from the chart.

```sh
# A released chart; its appVersion is the matching image tag.
helm install ot oci://ghcr.io/couchpartygames/charts/opentournament --version 1.2.3 \
  -n opentournament --create-namespace -f my-values.yaml

# From a checkout; installs the image built from main.
helm install ot deploy/helm/opentournament -n opentournament --create-namespace -f my-values.yaml
```

A minimal `my-values.yaml`:

```yaml
config:
  oidcIssuer: https://sso.example.com/realms/games
database:
  existingSecret: opentournament-db   # key OT_DATABASE_URL, or set existingSecretKey
catalog:
  games:
    - id: arena
      name: Arena
      identityKinds: [keycloak]
      maximumMatchSize: 2
      fleet: { name: arena, namespace: games }
      trustedClients: [arena-backend]
```

## Values

| Value | Default | Meaning |
|---|---|---|
| `config.oidcIssuer` | — | Keycloak realm URL. Required. |
| `config.gameServers` | `agones` | `fake` runs without Game Servers and skips RBAC. |
| `config.docs` | `false` | Serve API docs at `/api/v1/docs`. |
| `config.eventRetention` | `1h` | How long live-update events are kept. |
| `config.shutdownTimeout` | `30s` | Drain bound; keep below `terminationGracePeriodSeconds` (45). |
| `config.manifestNamespaces` | `[]` | Namespaces whose Tournament Manifests declare Tournaments; a Role to read them and update their status is added in each. Empty turns declared Tournaments off. |
| `database.url` / `database.existingSecret` | — | PostgreSQL connection string, inline or from a Secret. One is required. |
| `matchTokenKey.value` / `matchTokenKey.existingSecret` | generated | Base64 key (≥ 32 bytes) signing Match tokens. When neither is set, the chart generates one and keeps it across upgrades. |
| `catalog` | — | The Game catalog, as in [`examples/games.yaml`](../../../examples/games.yaml). At least one Game is required. |
| `env` / `envFrom` | `[]` | Extra environment, e.g. `OTEL_EXPORTER_OTLP_ENDPOINT`. |
| `replicaCount` | `2` | Replicas coordinate through PostgreSQL; a PodDisruptionBudget is added above 1. |
| `image.tag` | appVersion | Image tag of `ghcr.io/couchpartygames/opentournament`. |
| `ingress.*` | disabled | Standard Ingress; the live-update endpoint uses WebSockets. |
| `rbac.create`, `serviceAccount.*` | `true` | Set `rbac.create=false` to manage the Roles yourself. |

See [`values.yaml`](values.yaml) for the rest (resources, scheduling, security contexts).
