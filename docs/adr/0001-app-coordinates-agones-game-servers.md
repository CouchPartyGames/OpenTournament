# The tournament API coordinates Agones game servers itself

This application is both the source of truth for tournament state and the coordinator of game servers. It allocates one Game Server per Match from a warm Agones Fleet (one Fleet per Game) using a GameServerAllocation, then watches that GameServer's state to detect failures. We rejected a separate orchestrator service because the Match lifecycle and the server lifecycle are too tightly coupled (an Aborted Match is detected from server state) to split across services. We rejected creating a `GameServer` resource per Match because tournaments start in bursts: every first-round Match becomes Ready at once, and cold pod startup at that moment is unacceptable.

## Considered Options

- **A separate orchestrator service:** rejected, see above.
- **A `GameServer` resource per Match:** rejected, see above.
- **A full Kubernetes operator** (Tournaments, Stages and Matches as custom resources in etcd): rejected. etcd can't serve the queries and multi-object transactions the rules need (Capacity, uniqueness per Player Identity, completing a Match and unlocking the next one). Players and Game backends aren't Kubernetes users, so an API in front would be needed anyway. And the start-of-tournament burst would load the cluster's shared control plane. We keep only the controller *pattern*: a level-triggered reconcile loop between Matches in Postgres and GameServers in Agones. [ADR-0005](0005-tournament-manifests-declare-configuration-in-kubernetes.md) refines this: a Tournament's desired configuration may be declared in Kubernetes, while its runtime state stays in Postgres.

## Consequences

- The app needs Kubernetes RBAC to create GameServerAllocations and to get, list, watch, patch and delete GameServers, so it is tied to running inside (or with access to) an Agones cluster. Patch is needed because the app tells a running Game Server about a mid-Match Forfeit by annotating its GameServer, which the server sees through the Agones SDK. Delete is needed to release servers of Cancelled Matches and leaked servers.
- Every replica runs the reconcile loop, and Match state changes are conditional row updates, so exactly one replica acts on a failure without leader election. Because the loop is level-triggered and runs at startup, a GameServer that died while the app was down is still detected.
- Sizing the warm Fleet for the start-of-tournament burst is an operations concern outside this app.
