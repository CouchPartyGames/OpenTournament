# Tournament Manifests declare configuration in Kubernetes; runtime state stays in Postgres

Refines [ADR-0001](0001-app-coordinates-agones-game-servers.md).

Operators want recurring and scheduled Tournaments to live in git and reach the service through Argo CD or Flux, like the rest of their cluster. So a Tournament can be declared by a Tournament Manifest: a namespaced `Tournament` custom resource (`opentournament.io/v1alpha1`) whose spec holds only the desired **configuration**, the same settings `POST /api/v1/tournaments` takes, plus the Organizer. The service watches Manifests itself, creates the Tournament each one declares, and reports it in the Manifest's status. Registrations, Stages, Groups, Matches and Bouts stay in Postgres.

ADR-0001 rejected keeping Tournaments, Stages and Matches in etcd, and that still holds: none of its reasons apply to configuration. A Manifest is one small object that changes when a person edits git, not on every Registration or Bout. It needs no queries or multi-object transactions, since those rules run in Postgres when the Tournament is created from it. Only operators write Manifests, never Players or Game backends. And the start-of-Tournament burst touches Postgres, not the control plane. The Manifest's status holds only what points into Postgres (the Tournament's ID and current status), so the Manifest never becomes a second source of truth.

Every replica watches the Manifests, without leader election, as it already does for GameServers. A Tournament records the Manifest it came from (`tournament/<namespace>/<name>`) in a unique column. When several replicas, or one replica twice, reconcile the same Manifest, exactly one insert wins and the others return the Tournament it created. The status each replica writes is a function of the Manifest's generation and that Tournament, so concurrent writers converge, and optimistic concurrency on the Manifest's `resourceVersion` retries the losers. Leader election would add a Lease, failover delays and another RBAC grant, only to save a few duplicate reads.

## Considered Options

- **The whole Tournament in etcd** (a full operator): rejected again, for ADR-0001's reasons.
- **A separate controller calling the public API:** rejected. It would need its own Keycloak client trusted for every Game, it couldn't make creation idempotent without an API for that, and it would be another thing to deploy.
- **Leader election for the Manifest controller:** rejected, see above.

## Consequences

- The feature is opt-in: the service watches the namespaces listed in `OT_MANIFEST_NAMESPACES`, and with none it doesn't talk to the Kubernetes API for Manifests at all.
- The service needs RBAC to get, list and watch `tournaments.opentournament.io`, and to update `tournaments/status`, in every watched namespace.
- A Manifest's spec is validated twice: structurally by the CRD's OpenAPI schema, which mirrors the API's, and then by the same cross-field rules as the API when the Tournament is created.
- The Manifest's status follows the Tournament only as often as the controller resyncs, not in real time. Live updates remain the way to follow a Tournament.
- Git is the source of truth for a declared Tournament's settings. The service applies an edited Manifest to a Draft through the API's own edit path, and reports a Manifest it can't apply (frozen settings, a failed validation, an untrusted Organizer) in its `Synced` condition rather than retrying it. The API refuses to edit a declared Tournament (`409 declared-in-git`), but can still cancel it as an emergency lever; the Manifest then keeps pointing to the cancelled Tournament.
- Recurring schedules come later, and must keep runtime state out of etcd.
