# Agones Tournament C# SDK

Hand-written clients for **.NET 10 LTS only**, with nullable reference types enabled.
The solution contains four NuGet packages:

- `AgonesTournament.Sdk.Core`: caller-configured HTTP transport, UUID-backed Match,
  Tournament and Participant IDs, shared models, typed problem errors, and a live WebSocket connection.
- `AgonesTournament.Sdk.GameClient`: Tournament discovery, structure, Final Placements,
  and Participant registration using caller-supplied Keycloak access tokens.
- `AgonesTournament.Sdk.GameServer`: the full `/api/v1/game-server/*` API and
  allocation metadata through the official [Agones C# SDK](https://agones.dev/site/docs/guides/client-sdks/csharp/).

- `AgonesTournament.Sdk.Organizer`: Tournament creation and management for Organizers
  and trusted Game backends using caller-supplied access tokens.
Packages are built as CI artifacts; they are not published to a NuGet feed yet.

## Build and install locally

From the repository root, with the .NET 10 SDK installed:

```sh
dotnet restore sdks/csharp/AgonesTournament.Sdk.slnx
dotnet build sdks/csharp/AgonesTournament.Sdk.slnx -c Release --no-restore
dotnet test sdks/csharp/AgonesTournament.Sdk.slnx -c Release --no-build
dotnet pack sdks/csharp/AgonesTournament.Sdk.slnx -c Release --no-build -o sdks/csharp/artifacts
```

Download the `csharp-sdk-packages` artifact from CI, or use the local output
above as a NuGet source. Add `AgonesTournament.Sdk.GameServer` version `0.1.0`
to your application; Core and AgonesSDK are transitive dependencies:

```sh
dotnet nuget add source /absolute/path/to/packages --name agones-tournament-local
dotnet add package AgonesTournament.Sdk.GameServer --version 0.1.0
```

NuGet must also have nuget.org configured to restore the Agones dependencies.

## Game Client discovery and registration

Install `AgonesTournament.Sdk.GameClient` version `0.1.0` for your game client.
The caller owns sign-in. Supply a provider that returns a current Keycloak
access token; it is invoked on each call, with the call's cancellation token.

```csharp
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameClient;

using var http = new HttpClient();
var transport = new TournamentHttpClient(http, new Uri("https://tournament.example"));
var discovery = new GameClient(transport);
var page = await discovery.ListTournamentsAsync(
    gameId: "marbles", status: TournamentStatus.RegistrationOpen, limit: 50, offset: 0);
foreach (var tournament in page.Tournaments ?? [])
    Console.WriteLine($"{tournament.Id}: {tournament.Name} ({tournament.Registered}/{tournament.Capacity})");

// GetCurrentAccessTokenAsync is supplied by your application's Keycloak sign-in integration.
var client = new GameClient(transport, GetCurrentAccessTokenAsync);
var selected = (page.Tournaments ?? []).First();
var participant = await client.RegisterAsync(selected.Id); // Own Keycloak identity.
// To register a linked identity instead:
// var participant = await client.RegisterAsync(selected.Id,
//     new PlayerIdentity { Kind = "steam", Value = linkedSteamId });
var registrations = await client.MyRegistrationsAsync(selected.Id);
await client.CheckInAsync(selected.Id, participant.Id); // During the Check-in Window.
// await client.UnregisterAsync(selected.Id, participant.Id); // Before the start.
```

`TournamentAsync`, `StructureAsync` and `FinalPlacementsAsync` read settings,
Stages/Groups/Rounds/Matches/Bouts/Standings, and finishing ranges respectively.
Collections may be `null`; use `?? []` when enumerating. Listing defaults to 50
Tournaments; increase `offset` for subsequent pages (maximum `limit` is 200).

`MatchAsync(matchId)` returns `MatchDetails`. `ServerAddress` and `ServerPort`
are nullable: they are present only when the API includes them for a Participant
or Organizer. `ServerAllocated` alone does not mean an endpoint is visible.
Structure reads use `TournamentMatch` without endpoint fields; the Game Server
library's `Match` is its allocation-specific view.

Public reads work without a token provider. When a provider is supplied, public
reads also send its token, allowing `MatchAsync` to receive an authorized endpoint.
Registration, unregistration, Check-in and `MyRegistrationsAsync` require a
provider and fail with `InvalidOperationException` before HTTP if it is missing
or returns an empty token. Registering during the Check-in Window also checks in.
Registration and Check-in problem errors use the typed exceptions listed below.

## Organizer and Game backend management

Install `AgonesTournament.Sdk.Organizer` version `0.1.0`. Supply a provider for a
current Keycloak access token on each call. A Game backend typically obtains a
client-credentials token for a client listed in the Game's `trustedClients`.
The application owns token acquisition, caching and refresh.

```csharp
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.Organizer;

using var http = new HttpClient();
var transport = new TournamentHttpClient(http, new Uri("https://tournament.example"));
// Supplied by your backend's Keycloak client-credentials integration.
var organizer = new OrganizerClient(transport, GetCurrentAccessTokenAsync);
var start = DateTimeOffset.UtcNow.AddHours(2);
var tournament = await organizer.CreateTournamentAsync(new NewTournament
{
    GameId = "marbles",
    Name = "Weekly Tournament",
    Capacity = 16,
    MinimumParticipants = 2,
    RegistrationOpensAt = start.AddMinutes(-30),
    StartsAt = start,
    CheckIn = new CheckInSettings { Enabled = true, WindowSeconds = 300 },
    Stages = [new StageSettings
    {
        Format = MatchFormat.SingleElimination,
        BestOf = 3,
        ResultDeadlineSeconds = 600
    }]
});
// During the Registration Window, after verifying the player's identity:
var participant = await organizer.RegisterAsync(tournament.Id,
    new PlayerIdentity { Kind = "steam", Value = verifiedSteamId });
// During the Check-in Window, after the player confirms they are present:
await organizer.CheckInAsync(tournament.Id, participant.Id);
```

`EditTournamentAsync` replaces the Draft's complete settings using
`TournamentSettings`; the Game cannot be changed. Settings freeze when the
Registration Window opens. Declared Tournaments must be edited in git, but can
still be cancelled with `CancelTournamentAsync`. `TournamentAsync`,
`ListTournamentsAsync` (Game/status filters, limit 1–200, offset),
`ListParticipantsAsync`, and `DisqualifyAsync` cover reads and management.
Participant collections may be null; enumerate with `?? []`.

Registering and checking in on another player's behalf requires a trusted Game
backend token; being the Organizer alone does not grant identity ownership.
`RegisterAsync` requires an explicit `PlayerIdentity`. Registration during the
Check-in Window also checks in. Each client call uses a fresh provider token,
rejects empty tokens before HTTP, and accepts a `CancellationToken`.

Settings mirror the API and are validated by the service; failures include field
locations in `ValidationException.Problem.Errors`. The constraints are:

- Name: 1–200 characters; Capacity: 2–100000; Minimum Participants: at least 2
  and no more than Capacity. Start must be in the future and registration must
  open before the start. Enabled Check-in needs at least 60 seconds and must fit
  inside the Registration Window. Omit `CheckIn` to disable it.
- Stages: 1–10 in play order. Head-to-head Formats require `BestOf` of **1 or 3**
  and omit `Bouts`. Free-for-all requires `Bouts` of 1–100 and omits `BestOf`.
  Only Swiss may set nonzero `SwissRounds` (0–100; omitted/zero uses the default).
- Groups: 1–1024, defaults to 1. Result Deadline: 60–604800 seconds.
  Advancement is positive on every Stage followed by another Stage; on the last
  Stage omit it or use 0. Each Group needs at least two Participants and more
  Participants than its Advancement, including when only Minimum Participants
  register. Later Stages receive exactly the preceding Groups × Advancement.
  Free-for-all Groups must fit the Game's Maximum Match Size at Capacity.

## Waiting for the player's Game Server

After registering, wait for a Match allocation with the signed-in Game Client:

```csharp
using var waiting = new CancellationTokenSource(TimeSpan.FromMinutes(10));
try
{
    var endpoint = await client.WaitForNextMatchEndpointAsync(selected.Id, waiting.Token);
    await game.ConnectAsync(endpoint.Address, endpoint.Port);
}
catch (TournamentEndedException)
{
    // Completed or cancelled before a server became available.
}
```

`MyMatchesAsync(tournamentId, cancellationToken)` is an async stream of
`PlayerMatch` snapshots and changes for all of the caller's registered identities.
Its nullable `Endpoint` comes from private `match.changed` fields, or an authorized
REST Match read after resync. An Abort clears the old endpoint; the replacement
allocation yields a new one even for the same Match. Snapshot reconciliation
uses Abort counts and lifecycle/allocation progression to suppress buffered events
that would restore an older server or reopen a completed Match. Keep consuming the stream
when your game needs to follow replacement servers. Stalled, completed and cancelled
Matches have no connectable endpoint. The stream ends when the Tournament ends;
the helper throws `TournamentEndedException` if it ends before an endpoint arrives.
Both APIs require a token provider and honour cancellation.

For lower-level live updates, Core supports multiple subscriptions on one socket:

```csharp
await using var live = transport.CreateLiveConnection(GetCurrentAccessTokenAsync);
live.Subscribe(selected.Id);
await foreach (var notification in live.NotificationsAsync(cancellationToken))
{
    switch (notification)
    {
        case LiveResync resync:
            // Fetch state over REST now, before applying subsequent events.
            // resync.Sequence is the server's per-Tournament subscription baseline.
            break;
        case LiveEvent { Payload: MatchChanged match }:
            Console.WriteLine($"{match.MatchId}: {match.Status}");
            break;
        case LiveEvent { Payload: null } unknown:
            Console.WriteLine($"{unknown.Type}: {unknown.Data}");
            break;
    }
}
// live.Unsubscribe(selected.Id) queues removal while the reader is active.
```

Connections start when their single notification reader starts. Subscribe and
unsubscribe queue commands, and disposal or reader cancellation closes the socket.
Anonymous connections omit the provider; authenticated connections fetch a fresh
token on every reconnect and authenticate before subscribing. Each acknowledged
subscription emits `LiveResync`, including after a sequence gap or disconnection.
The offending gap event is discarded, duplicate/older events are suppressed, and
reconnect attempts back off from 250ms to 30s. Events received while REST is being
fetched are buffered in order; REST remains the source of truth. Server protocol
errors throw `LiveProtocolException` with the server's code; these errors require
caller action rather than automatic reconnects. Known events have typed `Payload`
records; every event retains its original `Type`, `Data`, Tournament and sequence,
including unknown event types.

## Minimal Game Server loop

Supply the **service root** URL (for example `https://tournament.example`), not
`/api/v1`. A deployment prefix such as `https://example.com/tournaments/` is
preserved. The caller owns `HttpClient` and the Agones SDK; the Tournament client
sets the Bearer token per request without changing shared default headers.

Once Agones has allocated the GameServer, read the assignment and fetch its
Match. Continue running Agones Ready/Health as usual. `agones` below is the
caller's `AgonesSDK`; `game` implements the game-specific methods shown in the
[compiled example](examples/MatchLoop/MatchLoop.cs).

```csharp
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

using var http = new HttpClient();
var transport = new TournamentHttpClient(http, new Uri("https://tournament.example"));
var metadata = new AgonesGameServer(agones);
var assignment = await metadata.AssignmentAsync(cancellationToken);
var client = new GameServerClient(transport, assignment.MatchToken);
using var forfeits = metadata.WatchForfeits(ids =>
{
    foreach (var id in ids)
        game.Kick(id);
});

var match = await client.MatchAsync(cancellationToken);
var winsNeeded = match.BestOf!.Value / 2 + 1;
var wins = (match.CompletedBouts ?? []).SelectMany(b => b.Results ?? []).Where(r => r.Won)
    .GroupBy(r => r.ParticipantId).ToDictionary(g => g.Key, g => g.Count());
var nextBout = (match.CompletedBouts ?? []).Select(b => b.Number).DefaultIfEmpty(0).Max() + 1;
await client.ReportStartedAsync(cancellationToken);
for (var bout = nextBout; !wins.Values.Any(count => count >= winsNeeded); bout++)
{
    var winner = await game.PlayHeadToHeadAsync(match, bout, cancellationToken);
    await client.ReportWinnerAsync(bout, winner, cancellationToken);
    wins[winner] = wins.GetValueOrDefault(winner) + 1;
}
await agones.ShutDownAsync();
```

This short loop is for head-to-head Matches. The compiled example also reports
No-shows. For free-for-all, play the remaining Bouts up to `match.Bouts` and
report each with `ReportPlacementsAsync`. Use `match.BestOf` for head-to-head or
`match.Bouts` for free-for-all; `Participants` carries Player Identities and
forfeited flags. `CompletedBouts` carries recorded winners, placements, points,
and Forfeits so a replacement server resumes only missing Bouts after an Abort.
The API permits `null` for collection fields; use `?? []` when enumerating.

Report No-shows with `ReportNoShowsAsync(bout, participantIds, cancellationToken)`.
For a free-for-all, use `ReportPlacementsAsync(bout, placements, cancellationToken)`
with a `BoutPlacement(participantId, placement, points)` for each Participant
still playing. The Game Server computes points. A head-to-head No-show can finish
the Bout or Match: fetch state again before continuing.

`WatchForfeits` reads the comma-separated `opentournament/forfeited` annotation
and calls back only for IDs not already delivered, including any present in the
first snapshot. Callbacks run on the Agones watch thread: marshal to your game
thread when necessary. Dispose the subscription to suppress callbacks. Agones
has no unregister operation and retains the delegate until its SDK is disposed.
`AssignmentAsync` reads `opentournament/match-id` and `opentournament/match-token`;
missing allocation metadata throws `InvalidOperationException`. Treat the token
as a secret; never log it. A replacement allocation receives a new token.

## Errors and retries

API failures throw `ApiException`, which exposes `Code`, `Detail`, `StatusCode`,
and the full `Problem` (including validation field errors and arbitrary JSON
field values). Specific problem codes have these subclasses:

| Problem code | Exception |
|---|---|
| `registration-closed` | `RegistrationClosedException` |
| `tournament-full` | `TournamentFullException` (Capacity reached) |
| `already-registered` | `AlreadyRegisteredException` |
| `identity-kind-not-accepted` | `PlayerIdentityKindNotAcceptedException` |
| `identity-not-owned` | `PlayerIdentityNotOwnedException` |
| `check-in-closed` | `CheckInClosedException` |
| `match-token-invalid` | `MatchTokenInvalidException` (invalid, expired, or superseded token) |
| `bout-conflict` | `BoutConflictException` |
| `bout-already-forfeited` | `BoutAlreadyForfeitedException` |
| `bout-out-of-order` | `BoutOutOfOrderException` |
| `settings-frozen` | `SettingsFrozenException` |
| `declared-in-git` | `DeclaredInGitException` |
| `not-trusted-for-game` | `NotTrustedForGameException` |
| `validation-failed`, `bad-request` | `ValidationException` |

Unknown codes remain `ApiException` with their original code and detail.
Non-problem HTTP errors (for example a proxy's HTML response) have code
`http-error` and preserve the HTTP status. Transport failures remain
`HttpRequestException`; cancellation remains `OperationCanceledException`.

```csharp
try
{
    await client.ReportWinnerAsync(bout, winner, cancellationToken);
}
catch (BoutConflictException error)
{
    // Reconcile the local result with the already recorded Bout.
    Console.Error.WriteLine($"{error.Code}: {error.Detail}");
}
```

While the allocation's token remains valid, identical repeated Bout reports
succeed through the API; reporting a different result throws a typed conflict.
Recorded Bout reports can also be retried after the Match completes, including
the deciding report, while the token remains valid for the latest allocation.
New Bouts are rejected after completion. Other operations (including fetching
the Match) reject the token after completion, so stop the loop after the deciding
report. A `MatchTokenInvalidException` alone cannot establish whether a lost
report was recorded. No automatic retries or local result cache are
used. If a request's outcome is unknown after a transport failure, retry the same
report. Pass a `CancellationToken` to HTTP operations to cancel the request;
`AssignmentAsync` cancellation stops waiting, while the caller's Agones SDK
controls the underlying RPC's lifetime.

## Contract checks

Tests use fake HTTP handlers, a local fake WebSocket server, and the real Agones SDK over an in-memory gRPC
transport. They verify all operation paths, methods, Bearer authentication,
request shapes, problem mapping, metadata, and callback lifetime. The contract
test reads the committed [`api/openapi.json`](../../api/openapi.json), checks
all shared model fields/types/required fields and enum values, and validates
outgoing bodies against the corresponding schemas. Regenerate the contract with
`make openapi` when changing the API.
