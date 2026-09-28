# Live updates go through a transactional outbox, fanned out with Postgres NOTIFY

Every live-update event is written to an `events` table in the same transaction as the change that caused it, which gives it a sequence number per Tournament. After commit, a `NOTIFY` carries only the Tournament ID and sequence number. Each replica then reads the new rows and forwards them to its own WebSocket subscribers. We rejected publishing straight after commit, because a crash between commit and publish silently loses the event. We rejected putting whole events in `NOTIFY` payloads, because the 8000-byte limit is too small for the Standings of a large free-for-all Group. And we rejected Redis (or any broker) pub/sub, to avoid running a second piece of infrastructure beside PostgreSQL.

## Consequences

- The server keeps no per-client replay buffer. Clients use the sequence numbers to spot gaps, and refetch state over REST when they reconnect or see one.
- The `events` table grows during a Tournament and needs pruning after a short retention period.
- If throughput ever outgrows `LISTEN`/`NOTIFY`, the outbox stays in place and only the notification transport needs replacing.
