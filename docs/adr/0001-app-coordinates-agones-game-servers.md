# The tournament API coordinates Agones game servers itself

This application is both the source of truth for tournament state and the coordinator of game servers. It allocates one Game Server per Match from a warm Agones Fleet (one Fleet per Game) using a GameServerAllocation, then watches that GameServer's state to detect failures. We rejected a separate orchestrator service because the Match lifecycle and the server lifecycle are too tightly coupled (an Aborted Match is detected from server state) to split across services. We rejected creating a `GameServer` resource per Match because tournaments start in bursts: every first-round Match becomes Ready at once, and cold pod startup at that moment is unacceptable.

## Consequences

- The app needs Kubernetes RBAC to create GameServerAllocations and watch GameServers, so it is tied to running inside (or with access to) an Agones cluster.
- Sizing the warm Fleet for the start-of-tournament burst is an operations concern outside this app.
