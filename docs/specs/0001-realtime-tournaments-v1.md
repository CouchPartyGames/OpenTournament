---
title: Real-time tournaments v1
labels: [ready-for-agent]
---

# Real-time tournaments v1

## Problem Statement

People running online-game tournaments with short notice (players register minutes before start) have no open-source backend that both manages the tournament *and* puts each Match on a real Game Server. Existing tools (start.gg, Challonge, Battlefy) are hosted products built around people reporting their own results. Their Participants have to report results themselves, Organizers have to settle disputes, and nothing links the bracket to the servers the Matches are played on. The result is slow, easy-to-cheat tournaments that need someone watching them the whole time, which doesn't work for tournaments that run in minutes to hours.

## Solution

An MIT-licensed Go backend API that serves as the source of truth for real-time Tournaments and also coordinates the Agones Game Servers the Matches run on. An Organizer (a person or a trusted Game backend) schedules a Tournament for a Game, and then it runs itself:

- Registration opens, an optional Check-in Window runs at the end of it, and the Tournament starts on schedule.
- Every Stage is generated and seeded, and each Match gets a Game Server from a warm Fleet as soon as its Participants are known.
- Game Servers report each Bout result, and Standings, Advancement, later Stages and Final Placements follow automatically.
- Crashed servers are handled without losing completed Bouts.
- The Organizer steps in only to cancel, disqualify, or resolve a Stalled Match.

Frontends and game backends read everything from the API and receive live updates.

See `CONTEXT.md` for the vocabulary used throughout, and ADR-0001 and ADR-0002 for the key architectural decisions.

## User Stories

### Tournament creation and configuration

1. As an Organizer, I want to create a Tournament for a specific Game, so that its Game Servers and accepted Player Identities are known.
2. As an Organizer, I want to set a start time, so that the Tournament begins automatically without me being present.
3. As an Organizer, I want to set when the Registration Window opens, so that players can sign up shortly before start.
4. As an Organizer, I want to set a Capacity, so that the Tournament never exceeds what I planned for.
5. As an Organizer, I want to set Minimum Participants, so that a Tournament with too few players is cancelled rather than run.
6. As an Organizer, I want to enable or disable Check-in, so that I can require presence confirmation only when I need it.
7. As an Organizer, I want to set the length of the Check-in Window, so that it fits the pace of my Tournament.
8. As an Organizer, I want to define one or more Stages in order, so that I can run formats such as "Swiss into single elimination".
9. As an Organizer, I want to pick a Format for each Stage (single elimination, double elimination, round robin, Swiss, free-for-all), so that each phase plays the way I intend.
10. As an Organizer, I want to split a Stage into several Groups, so that many Participants can play in parallel pools.
11. As an Organizer, I want to set how many Participants per Group advance to the next Stage, so that Advancement is predictable.
12. As an Organizer, I want to choose Best-of 1 or Best-of 3 for head-to-head Stages, so that important Stages are decided more fairly.
13. As an Organizer, I want to set the number of Bouts in a free-for-all Match, so that battle-royale results sum across several drops.
14. As an Organizer, I want to override the number of Swiss Rounds, so that I can shorten or lengthen a Swiss Stage.
15. As an Organizer, I want to set a Result Deadline per Stage, counted from when each Match becomes Ready, so that stuck Matches are flagged instead of blocking the Tournament forever.
16. As an Organizer, I want the configuration validated when I create the Tournament (e.g. free-for-all Group larger than the Game's Maximum Match Size, Advancement larger than a Group, or any Group ending up with fewer than Advancement + 1 Participants when only Minimum Participants show up), so that I never discover an impossible Tournament at start time.
17. As an Organizer, I want to edit settings while the Tournament is still a draft, so that I can fix mistakes before anyone registers.
18. As an Organizer, I want settings to be frozen once registration opens, so that Participants can trust the rules they signed up under.
19. As a Game backend, I want to create Tournaments through a trusted client, so that I can schedule recurring cups (e.g. hourly) automatically.
20. As a Game backend that created a Tournament, I want to be its Organizer, so that I can manage it like a human Organizer.

### Registration and check-in

21. As a player logged in through Keycloak, I want to register myself for a Tournament during the Registration Window, so that I can compete.
22. As a Game backend, I want to register a player by their Player Identity (e.g. Steam ID or game account), so that players without a Keycloak account can compete.
23. As a player, I want registration to be refused if my Player Identity isn't a kind the Game accepts, so that I don't end up in a Tournament whose Game Server can't recognize me.
24. As a player, I want registration to be first come, first served up to the Capacity, so that the rules are clear and fair.
25. As a player, I want a clear refusal when the Tournament is full, so that I can look for another one.
26. As a player, I want registration to be refused outside the Registration Window, so that the Participant list is stable at start.
27. As a player, I want to be prevented from registering twice in the same Tournament, so that the field has no duplicates.
28. As a player, I want to unregister before the Tournament starts, so that I free up my spot if I can't play.
29. As a player, I want to check in during the Check-in Window, so that I keep my spot.
30. As a player who registers during the Check-in Window, I want to be checked in automatically, so that I don't need to take a second step.
31. As a Game backend, I want to check in a player on their behalf, so that in-game presence counts as a check-in.
32. As a player, I want to see whether I'm registered and checked in, so that I know my spot is safe.
33. As an Organizer, I want registered Participants who didn't check in to be dropped at start, so that the Stages contain only players who are present.
34. As a player, I want to register for several Tournaments that overlap in time, so that I am not blocked by the platform (and I accept the risk of Forfeits).

### Tournament lifecycle

35. As an Organizer, I want the Tournament to open registration automatically at the configured time, so that I don't need to be online.
36. As an Organizer, I want the Tournament to start automatically at the start time, so that the tempo is kept.
37. As an Organizer, I want a Tournament below Minimum Participants at start to be cancelled automatically, so that nobody waits for a Tournament that can't run.
38. As an Organizer, I want to cancel a Tournament at any point before it completes, so that I can stop an event that went wrong.
39. As a Participant, I want a cancelled Tournament's pending Matches to stop and its Game Servers to be released, so that nobody keeps playing for nothing.
40. As an Organizer, I want the next Stage to begin automatically when every Group of the current Stage is complete, so that the Tournament keeps moving.
41. As an Organizer, I want the Tournament to complete automatically when the last Stage is complete, so that Final Placements appear immediately.
42. As anyone, I want to see a Tournament's current status (Draft, Registration Open, Check-in, Running, Completed, Cancelled), so that I know what's happening.

### Stage generation, Seeding and Groups

43. As a Participant, I want the first Stage seeded randomly, so that nobody gets a hand-picked easy path.
44. As a Participant, I want later Stages seeded by Standing from the previous Stage, so that doing well earlier is rewarded.
45. As a Participant, I want Group winners seeded above runners-up and placed across Groups where possible, so that I don't immediately meet someone from my own Group.
46. As an Organizer, I want Participants distributed into Groups by snake seeding, so that Group sizes differ by at most one and strength is balanced.
47. As a top-seeded Participant in an elimination Stage whose count is not a power of two, I want a Bye, so that the bracket works for any count.
48. As a Participant in a Swiss Stage with an odd count, I want a Bye (worth a win) to go to the lowest-standing Participant who hasn't had one yet, so that Byes are fair.
### Formats and Match flow

49. As a Participant in an elimination Stage, I want my next Match to become Ready as soon as both of its feeding Matches are done, so that I don't wait for unrelated Matches. If a feeding Match ends in a double Forfeit, I get a Bye past that Match.
50. As a Participant in a Swiss or round robin Stage, I want the next Round to begin once every Match of the current Round is done, so that pairings use complete results.
51. As a Participant in a Swiss Stage, I want to be paired against opponents with similar points whom I haven't played yet, so that Swiss works correctly.
52. As a Participant in a round robin Group, I want to play every other Participant in my Group exactly once, so that the Standing is complete.
53. As a Participant in double elimination, I want a loss in the upper bracket to drop me to the lower bracket, so that one loss doesn't eliminate me.
54. As a lower-bracket finalist who beats the undefeated upper-bracket finalist, I want a Bracket Reset, so that both finalists are eliminated only after two losses.
55. As a Participant in a free-for-all Group, I want my whole Group to play as one Match, so that it behaves like a battle-royale lobby.
56. As a Participant in a best-of-3 Match, I want the Match to end as soon as someone has won two Bouts, so that no pointless Bouts are played.
57. As a Participant in a free-for-all Match, I want my points summed across its Bouts, so that consistency is rewarded.
58. As a Participant in a head-to-head Match, I want draws to be impossible, so that every Match has a winner.

### Game Servers

59. As a Participant, I want a Game Server allocated for my Match as soon as it becomes Ready, so that I can play immediately.
60. As an operator, I want Game Servers allocated from a warm Fleet per Game, so that the burst of first-Round Matches at start doesn't wait for servers to boot.
61. As a Participant, I want the address and port of my Match's Game Server, and I want only that Match's Participants and the Organizer to be able to see it, so that I can connect and nobody else can target the server.
62. As a Participant, I want only the Match's Participants admitted to the Game Server, so that nobody else can join or interfere.
63. As a Game Server, I want a per-Match token when I'm allocated, so that I can authenticate to the API for that Match only.
64. As a Game Server, I want to fetch my Match (Participants, Player Identities, Best-of or Bout count, Bouts already completed) using my token, so that I can set up the right session.
65. As a Game Server, I want to report that the Match has started, so that the Match shows as In Progress.
66. As a Game Server, I want to report each Bout's result (winner for head-to-head, or placement and points per Participant for free-for-all), so that Standings update live.
67. As a Game Server, I want to report a No-show for a Bout, which counts as a Forfeit of that Bout, so that a missing Participant doesn't block the Match.
68. As a Game Server, I want my token rejected for any other Match and after my Match ends, so that a compromised server can't change other results.
69. As a Participant, I want the Game Server released once the Match completes, so that capacity is recycled for other Matches.
70. As a Participant, I want allocation retried (with backoff) when no warm server is available, even across a service restart, so that my Match still starts once capacity frees up.
71. As a Participant whose Game Server crashes mid-Match, I want the Match Aborted and a new server allocated, so that the Match can still be finished. Its Result Deadline keeps running, so a server that keeps crashing ends in Stalled rather than looping forever.
72. As a Participant in an Aborted Match, I want completed Bouts kept and only missing Bouts replayed, so that a crash doesn't erase my lead.

### Results, Standings and Placements

73. As a spectator or Participant, I want Standings per Group updated as each result arrives, so that I can follow the Tournament live.
74. As a Participant, I want ties in a Standing broken by the fixed Tiebreaker order for the Format, so that Advancement is deterministic and explainable.
75. As a Participant in Swiss, I want Tiebreakers in the order points, Buchholz, head-to-head, random, so that stronger opposition counts.
76. As a Participant in round robin, I want Tiebreakers in the order points, head-to-head, Bout differential, random.
77. As a Participant in free-for-all, I want Tiebreakers in the order total points, best single placement, random.
78. As a Participant, I want the top N per Group to advance automatically once the Group completes, so that the next Stage fills without anyone stepping in.
79. As a Participant, I want a Final Placement when the Tournament completes, so that I know where I finished.
80. As a Participant knocked out in the same elimination Round as others, I want a shared placement range (e.g. 5th–8th), so that placements aren't arbitrary. Both Participants of a double Forfeit share the range of the Round they were knocked out in.
81. As a Participant knocked out in an earlier Stage, I want my Final Placement to come from the Stage I reached, ranked below everyone who advanced further.

### Deadlines and Organizer intervention

82. As an Organizer, I want a Match with no result by its Result Deadline flagged as Stalled, so that I notice it.
83. As an Organizer, I want to resolve a Stalled Match by awarding a win or a double Forfeit, so that the Tournament can continue.
84. As an Organizer, I want resolving a Stalled Match to be the only manual way to set a result, so that results can't be quietly tampered with.
85. As an Organizer, I want to disqualify a Participant, so that I can remove cheaters or abusive players.
86. As a Participant, I want to withdraw from a running Tournament, so that I can leave cleanly.
87. As an Organizer, I want a withdrawn or disqualified Participant to forfeit every Bout they have not yet completed, including those in a Match already In Progress, so that the brackets keep moving.
88. As a Participant in Swiss, I want withdrawn or disqualified players excluded from future pairings, while their past results still count toward others' Buchholz, so that tiebreaks stay fair.

### Reading and live updates

89. As anyone, I want to list public Tournaments with their Game, status and start time, so that I can find one to join or watch.
90. As anyone, I want a Tournament's full structure (Stages, Groups, Rounds, Matches, Bouts), so that a frontend can draw brackets and tables.
91. As anyone, I want to subscribe to live updates for a Tournament (registrations, status changes, Match state changes, Bout results, Standings), so that frontends don't have to poll.
92. As a Participant, I want to receive a live update when my Match is Ready and its Game Server address is known, so that I can join immediately. Public updates show only that a Match is allocated or In Progress, never the address.
93. As a frontend developer, I want error responses as Problem Details (RFC 9457) with specific error codes, so that I can show meaningful messages.

### Operators and contributors

94. As an operator, I want to register Games (their accepted Player Identity kinds, Maximum Match Size, Agones Fleet, trusted client) through configuration, so that no admin role or UI is needed.
95. As an operator, I want liveness, readiness and startup health checks, so that the service runs reliably in Kubernetes.
96. As an operator, I want traces, metrics and logs over OpenTelemetry, so that I can diagnose problems in live Tournaments.
97. As an operator, I want the service to recover time-based transitions and deadlines after a restart, so that a pod restart doesn't freeze a Tournament.
98. As a contributor, I want the Format logic isolated in a pure module with its own tests, so that I can add or fix Formats safely.

## Implementation Decisions

### Platform and architecture

- Go (current stable release), HTTP served by the standard library `net/http` with `ServeMux` method and path patterns. The code is organized as vertical slices: one package per feature. Each handler decodes its input, calls one command or query function, and does nothing else.
- Expected failures (validation, not found, conflicts, wrong status for the operation) are returned as ordinary Go `error` values, never as panics. Each kind of failure has a typed domain error that carries its specific error code. Each slice validates its own input explicitly and returns every field error at once. A single central mapper turns errors into Problem Details (RFC 9457) responses, and any unrecognized error becomes a 500.
- PostgreSQL through `pgx`. SQL is written by hand and compiled into type-safe Go with `sqlc`, with one set of queries for the tournament bounded context. Read queries select straight into response DTOs. There is no ORM. Every schema change is a versioned SQL migration (`goose`). IDs are distinct named types (e.g. `TournamentID`, `MatchID`) over version-7 UUIDs, so they cannot be mixed up.
- The URLs are versioned (`/api/v1/...`). An OpenAPI 3.1 document is exposed, with Scalar in development.
- OpenTelemetry (Go SDK) covers traces, metrics and logs. Logging uses `log/slog`, bridged to OpenTelemetry. Health checks are split into liveness (`/livez`), readiness (`/readyz`) and startup (`/startupz`).
- Every long-running operation takes a `context.Context`, and the process shuts down gracefully on SIGTERM: it stops accepting requests, drains in-flight work and background workers, then exits.

### Modules

1. **Format engine** (deep, pure, no I/O: a single package that imports nothing that touches the database, network or clock). Its input is a Stage definition, the Group's seeded Participants and the results so far. Its outputs are:
   - Matches to create (for Round 1, for the next Round, or when a bracket slot becomes playable)
   - Match readiness (per-Match for elimination, per-Round for Swiss and round robin)
   - Standings with Tiebreakers applied
   - Group completion
   - the Advancing Participants
   - Final Placement ranges

   It covers single elimination (Byes), double elimination (lower bracket, Bracket Reset), round robin, Swiss (pairing without rematches, Byes, ⌈log₂ N⌉ default Rounds) and free-for-all (one Match per Group, summed points). Randomness is passed in, so the engine's outputs are fully determined by its inputs. It also computes Seeding and snake Group distribution.
2. **Tournament aggregate and lifecycle.** Status moves Draft → Registration Open → Check-in (only if enabled) → Running → Completed, and Cancelled is reachable from any status before Completed. It enforces the settings freeze, Capacity, Minimum Participants, accepted Player Identity kinds and uniqueness per Player Identity. It also creates Stages and Groups by calling the Format engine, and advances Stages.
3. **Match lifecycle.** The states are Pending → Ready → Allocating → In Progress → Completed.
   - Aborted leads back to Allocating, with completed Bouts kept.
   - Stalled is reached when the Result Deadline passes, and ends in Completed through the Organizer's resolution. The Result Deadline is set per Stage, counted from Ready, and not reset by an Abort.
   - A Forfeit is recorded per Bout, and the Match result follows from its Bouts (see Result rules).
   - Completing a Match hands its result back to the Format engine to unlock the next Matches or Rounds.
4. **Game Server coordination (port plus Agones adapter).** The port is a narrow Go interface: allocate for a Match (Game → Fleet), watch server state, and release. The Agones adapter uses the official Agones Go client and `client-go` informers. It creates GameServerAllocations against the Game's Fleet, passes the Match ID as allocation metadata, and watches the allocated GameServer. If the server becomes Unhealthy or is deleted before the Match completes, the Match is Aborted. Every replica watches GameServers. Each state change is a conditional update of the Match row: for example, move In Progress to Aborted only if the Match still points at that allocation. Exactly one replica's update succeeds, and the others are no-ops, so no leader election is needed. Allocation is a persisted scheduler job ("allocate Match X, due at T"). A failed attempt reschedules the job with backoff, so retries survive a restart. Kubernetes RBAC is limited to GameServerAllocations plus reading and watching GameServers. See ADR-0001.
5. **Match tokens.** The service issues a signed, short-lived token (a JWT signed with a service-held key) when it allocates a server, scoped to a single Match ID. It is separate from Keycloak-issued tokens and is accepted only on the Game Server endpoints for that Match while the Match isn't yet Completed or Cancelled. It is passed to the Game Server in allocation metadata. See ADR-0002.
6. **Scheduler.** A background worker goroutine, started with the server and stopped through its context, drives time-based and retried work: registration opens, the Check-in Window opens, the Tournament starts (dropping Participants who didn't check in, and cancelling below Minimum Participants), Game Server allocation attempts, and Result Deadlines. It works from persisted due times rather than in-memory timers, so a restart loses nothing, and it is safe with more than one replica (claim due work with `SELECT … FOR UPDATE SKIP LOCKED`). It uses an injected clock interface.
7. **Live updates.** A WebSocket endpoint on which clients subscribe to and unsubscribe from Tournaments. Domain changes are published after the transaction commits: registration counts, status, Match state, Bout results and Standings. The Game Server address goes only to that Match's Participants and the Organizer, never in public updates. Events are fanned out across replicas with PostgreSQL `LISTEN`/`NOTIFY`, so a client receives every update no matter which replica made the change.
8. **Game catalog.** Games come from configuration: Game ID and name, accepted Player Identity kinds, Maximum Match Size, Agones Fleet name and namespace, and the Keycloak client IDs trusted to act for the Game.

### Authentication and authorization

- Keycloak issues JWT bearer tokens. The service verifies them against Keycloak's OIDC discovery document and JWKS.
- **Human users:** any authenticated user can create a Tournament, and becomes its Organizer. A human can register or check in only their own Keycloak Player Identity, or a linked identity of an accepted kind if Keycloak exposes it as a claim.
- **Game backends:** a Keycloak service client listed as trusted for a Game can create Tournaments for that Game (becoming the Organizer) and can register or check in any Player Identity of an accepted kind.
- **Organizer actions** (cancel, disqualify, resolve Stalled, edit a draft) require being the Tournament's Organizer.
- **Game Server endpoints** accept only a Match token.

### Main API surface (all under `/api/v1`)

- Tournaments: create, edit a draft, get, list, cancel.
- Registrations: register, unregister, check in, list Participants.
- Participants: withdraw (self), disqualify (Organizer).
- Structure: get Stages, Groups, Rounds, Matches and Standings. Get Final Placements.
- Matches: get one (including the Game Server address for its Participants), resolve Stalled (Organizer).
- Game Server endpoints (Match token): get my Match, report Match started, report a Bout result, report a No-show.
- Live updates (WebSocket): subscribe to or unsubscribe from a Tournament.

### Result rules

- **Head-to-head Bout result:** the winning Participant, no draws. The Match completes when one Participant reaches the majority of the Best-of.
- **Free-for-all Bout result:** a placement and points for every Participant in the Match. Placements are strict, and the Game Server computes the points (the platform only sums them). The Match completes after the configured number of Bouts.
- **Forfeit:** recorded per Bout. A No-show reported by the Game Server forfeits that Bout. A Withdrawal or Disqualification forfeits every Bout the Participant has not yet completed, including those in a Match already In Progress. In head-to-head, a forfeited Bout is won by the opponent, so a Participant leading 1–0 in a best-of-3 who withdraws loses 1–2, and the Match completes right away. In free-for-all, a forfeited Bout scores last placement and no points, and the Match continues for everyone else. A double Forfeit (Organizer resolving a Stalled Match) means both Participants lose. In elimination, the next Match's opponent gets a Bye.
- **Validation at creation:** besides the per-field rules, the configuration is rejected if any Group could have fewer than Advancement + 1 Participants when attendance equals Minimum Participants.
- **Duplicates and conflicts:** reporting a Bout number that is already recorded is idempotent if the data is identical, and a conflict otherwise.

### Data

- **Persisted:** Tournaments, Stages, Groups, Rounds, Matches, Match slots (the Participants in a Match), Bouts, Bout results per Participant, Participants (with Player Identity kind and value, registration and check-in times, and status: registered, checked in, active, withdrawn, disqualified, eliminated), game-server allocations, and scheduled due times.
- **Computed or cached:** Standings and Final Placements are computed by the Format engine from persisted results. Caching them is an optimization, not the source of truth.

## Testing Decisions

- **What makes a good test:** it checks external behavior through a public seam: HTTP responses, published live updates, and calls to the Game Server port. It never checks unexported types, `sqlc` row types or database rows directly. Tests read like Tournament scenarios in the `CONTEXT.md` vocabulary, and use the standard `testing` package with table-driven cases where they fit.
- **Seam 1: the HTTP API (primary).** Integration tests host the real app in-process (`net/http/httptest`) against a real PostgreSQL (`testcontainers-go`), with a test JWT issuer in place of Keycloak. They cover:
  - creation and validation
  - the registration, Capacity and Check-in rules
  - lifecycle transitions
  - authorization
  - Game Server endpoints with Match tokens
  - Aborted and Stalled flows
  - Withdrawal and Disqualification
  - full multi-Stage Tournaments from start to Final Placements
- **Seam 2: the Format engine (pure).** Direct unit tests of the combinatorics:
  - Byes for every Participant count
  - the double-elimination lower-bracket flow and Bracket Reset
  - Swiss pairing without rematches and with odd-count Byes
  - round robin completeness
  - Tiebreaker ordering per Format
  - snake Group distribution
  - Advancement and cross-Group seeding
  - Final Placement ranges

  Property-style tests (e.g. with `pgregory.net/rapid`) and native Go fuzz tests are encouraged, for example "every Participant plays exactly once per Round" and "a single-elimination bracket always yields N−1 Matches".
- **Controlled test doubles** (these replace dependencies; they aren't extra seams):
  - **Fake Game Server port:** records allocations and releases, and lets a test simulate allocation failure, a server becoming Unhealthy, or its deletion.
  - **Fake clock:** lets tests move time forward to trigger scheduler transitions and deadlines.
  - **Seeded random source** (a seeded `math/rand/v2` source): gives repeatable Seeding and Swiss random tiebreaks.
- **Agones adapter:** not covered by automated tests in v1. A manual smoke test against a local cluster (for example kind with Agones installed) is documented instead.
- **Prior art:** none. This is a new repository, so these tests set the conventions.

## Out of Scope

- Frontends of any kind (the API serves them, but they aren't built here).
- Teams or rosters. A Participant is always one person.
- Spectator and judge roles, and any platform admin role or UI. Games are registered through configuration.
- Participants reporting their own results, and any dispute flow.
- Draws in any Format.
- Waitlists, invite-only or private Tournaments.
- Configurable Tiebreakers, a third-place Match, and making the Bracket Reset optional.
- Seeding by rating, and manual Seeding by the Organizer.
- Preventing players from entering overlapping Tournaments.
- Creating Agones Fleets and sizing or autoscaling them (an operations concern).
- Ladders or leagues with no fixed end, prizes and payments.
- Best-of values other than 1 and 3.

## Further Notes

- The vocabulary comes from `CONTEXT.md` and should be used in code as well (type names, routes, event names).
- ADR-0001: this app coordinates Agones itself, allocating from a warm Fleet per Game.
- ADR-0002: only Game Servers report results, each using a token scoped to one Match.
- Tournaments start in bursts: every first-Round Match becomes Ready at the same moment. Allocation and live update publishing must handle that burst without serializing on a single lock.
- Suggested first slice: create a Tournament, register Participants, start it, and generate a single-elimination Stage. Then report Bout results through the fake Game Server port all the way to Final Placements. Other Formats, Check-in, Groups and multi-Stage support come after.
