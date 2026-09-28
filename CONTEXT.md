# Open Tournament

An open-source backend for creating and running real-time tournaments for online games, where players register shortly before the tournament starts.

## Language

### People

**Organizer**:
The person who creates a Tournament, configures it, and runs it.
_Avoid_: Host, admin, owner

**Participant**:
A single individual entered into one Tournament. Always one person, never a team.
_Avoid_: Player, entrant, competitor, team

**Player Identity**:
The external account that identifies a Participant, such as a Keycloak user, a Steam ID, or a game-specific account. It is not required to be a Keycloak user.
_Avoid_: User, account, login

### Structure

**Game**:
The video game a Tournament is played in, e.g. Fortnite. It sets the Maximum Match Size and which kinds of Player Identity may register.
_Avoid_: Title, app, integration

**Maximum Match Size**:
The most Participants one Match of a Game can hold, e.g. 100 for a Fortnite lobby. A free-for-all Group can never be larger than this.
_Avoid_: Lobby size, server capacity

**Tournament**:
One competitive event for one online game. It has a fixed start and is made up of one or more Stages.
_Avoid_: Event, competition, cup

**Stage**:
One phase of a Tournament that is played in a single Format (e.g. a Swiss stage followed by a single-elimination stage).
_Avoid_: Phase, round, bracket

**Format**:
The rules a Stage uses to decide who plays whom and who advances: single elimination, double elimination, round robin, Swiss, or free-for-all.
_Avoid_: Type, mode, bracket type

**Group**:
A pool of Participants inside a Stage that plays its Format independently of, and in parallel with, the Stage's other Groups. A Stage with no split has exactly one Group.
_Avoid_: Pool, division, bracket, lobby

**Match**:
One contest between two or more Participants inside a Group: a head-to-head (A vs B) or a free-for-all (many Participants at once).
_Avoid_: Game, fixture, heat, lobby

**Game Server**:
The running server instance on which one Match is played from its first Bout to its last. Players connect to it; it reports each Bout result.
_Avoid_: Lobby, instance, host, room

**Bout**:
One play session within a Match. A head-to-head Match is decided by who wins most Bouts; a free-for-all Match by total points across its Bouts.
_Avoid_: Game, map, set, session, drop

**Best-of**:
The number of Bouts in a head-to-head Match: either 1 or 3. A best-of-3 is won by the first Participant to win two Bouts.
_Avoid_: Series, Bo-N

**Round**:
One step of a Stage in which a set of Matches is played, e.g. Swiss round 3 or the quarterfinals.
_Avoid_: Bout, wave, column

**Bye**:
An automatic advancement past a Match that has no opponent, for any reason: an elimination Stage whose Participant count is not a power of two, an odd count in a Swiss Round, or an opponent slot emptied by a double Forfeit.
_Avoid_: Walkover, free win

### Progression

**Seeding**:
The ordering of Participants that decides where they are placed in a Stage and which Group they join. The first Stage is seeded randomly; later Stages are seeded by Standing from the previous Stage.
_Avoid_: Ranking, draw

**Standing**:
A Participant's position within a Group, based on its Match results so far.
_Avoid_: Rank, leaderboard position, score

**Tiebreaker**:
An ordered rule used to separate Participants with equal points in a Standing (e.g. Buchholz, head-to-head). Each Format has a fixed default order.
_Avoid_: Tiebreak criteria

**Bracket Reset**:
A second grand final in double elimination, played when the lower-bracket finalist beats the previously undefeated upper-bracket finalist.
_Avoid_: Grand final 2, true final

**Final Placement**:
A Participant's finishing position in the whole Tournament. Participants knocked out in the same elimination Round share a range, e.g. 5th–8th.
_Avoid_: Final rank, result, prize position

**Advancement**:
The rule that moves the top N Participants of each Group, by Standing, into the next Stage, where they are seeded again.
_Avoid_: Qualification, promotion, progression

**Forfeit**:
A Participant not completing a Bout, through a No-show, Withdrawal, Disqualification, or the Organizer's resolution of a Stalled Match. A forfeited Bout is lost in head-to-head, and scores last placement with no points in free-for-all.
_Avoid_: Walkover, default loss, DQ

**No-show**:
A Participant who does not turn up on the Game Server for a Bout, as reported by the Game Server.
_Avoid_: Absent, AFK, missing

**Withdrawal**:
A Participant leaving a running Tournament of their own accord. They forfeit every Bout they have not yet completed.
_Avoid_: Drop, quit, leave

**Disqualification**:
The Organizer removing a Participant from a running Tournament. They forfeit every Bout they have not yet completed.
_Avoid_: DQ, ban, kick

**Aborted**:
A Match whose Game Server failed mid-play. It returns to be played again; Bouts already completed are kept and only the missing Bouts are replayed.
_Avoid_: Crashed, failed, restarted

**Stalled**:
A Match that has produced no result by its Result Deadline. Only the Organizer can resolve it, by awarding a win or a double Forfeit.
_Avoid_: Stuck, timed out, expired

**Result Deadline**:
The time a Match has to complete, counted from the moment it becomes Ready and set per Stage. An Abort does not reset it.
_Avoid_: Timeout, match timer, expiry

### Registration

**Registration Window**:
The period, shortly before a Tournament starts, during which Participants may register.
_Avoid_: Signup period, lobby

**Capacity**:
The maximum number of Participants a Tournament accepts.
_Avoid_: Max players, slots, size

**Check-in**:
An optional step where a registered Participant confirms they are present before the Tournament starts. Participants not checked in at start are dropped.
_Avoid_: Confirmation, ready-up

**Check-in Window**:
The final stretch of the Registration Window, ending at the start time, during which Check-in is open. Registering inside it counts as checking in.
_Avoid_: Check-in period, ready phase

**Minimum Participants**:
The smallest number of Participants a Tournament needs to start. Below it at start time, the Tournament is cancelled.
_Avoid_: Min players, quorum
