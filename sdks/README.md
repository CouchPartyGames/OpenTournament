# Client SDKs

This directory is reserved for Agones Tournament client libraries for C#, Go,
and TypeScript. These libraries will wrap the service's HTTP API so game clients,
Game backends, and Game Servers can work with typed requests and responses.

**Status:** The [C# SDK](csharp/README.md) implements the Game Server Match
lifecycle on .NET 10 LTS. CI keeps its NuGet packages as build artifacts; no
packages are published yet. Go and TypeScript clients are planned.

| Language | Planned directory | Intended use |
|---|---|---|
| C# | [`csharp/`](csharp/README.md) | Game Server Match lifecycle on .NET 10 LTS. |
| Go | `go/` | Game backends, services, and Go Game Servers. |
| TypeScript | `typescript/` | Browser tournament interfaces and Node.js applications. |

## API coverage

Over time, the clients will provide typed access to the same API:

- Create, edit, list, inspect, and cancel Tournaments.
- Register Participants, check in, and inspect registrations.
- Read Stages, Groups, Standings, and Matches.
- Fetch a Game Server's assigned Match and report its start, Bout results, and
  No-shows.

Tournament rules, scheduling, advancement, and Game Server allocation run in the
service. See the [domain vocabulary](../CONTEXT.md) for the terms used by the API.

## Connecting and authenticating

The REST API lives under `/api/v1`, for example
`http://localhost:8080/api/v1/tournaments` during local development.

Authenticated Player, Organizer, and Game backend requests use a Keycloak access
token in the `Authorization: Bearer <token>` header. The calling application
obtains the token and supplies it to the client.

Game Server requests under `/api/v1/game-server/*` use the per-Match token from
the allocated Agones GameServer's `opentournament/match-token` annotation. This
token authorizes only its assigned Match; these endpoints do not accept Keycloak
tokens. See [Game Servers](../README.md#game-servers) for the reporting flow.

## Live updates

Live updates use a separate WebSocket at `/api/v1/live`. A REST client alone does
not provide subscriptions. Applications can subscribe to Tournament events and
fetch state through REST; gaps in event sequence numbers require fetching state
again. See the [live-update protocol](../README.md#live-updates) for authentication
and subscription messages.

## API contract

The running service exposes its OpenAPI 3.1 contract at `/api/v1/openapi.json`.
Use the contract from the service version you are targeting when generating or
implementing a client. Interactive API documentation is available at
`/api/v1/docs` when `OT_DOCS=true`.

Once implemented, each language directory should document its installation,
supported runtime versions, client configuration, authentication, error handling,
and usage examples. See the [main README](../README.md) for running the service.
