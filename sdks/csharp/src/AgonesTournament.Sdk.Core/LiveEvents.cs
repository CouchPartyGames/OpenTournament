using System.Text.Json;

namespace AgonesTournament.Sdk.Core;

/// <summary>One ordered live notification for a Tournament.</summary>
/// <param name="TournamentId">The subscribed Tournament.</param>
public abstract record LiveNotification(TournamentId TournamentId);

/// <summary>Fetch state over REST before applying subsequent events. Sequence is the server's subscription baseline.</summary>
public sealed record LiveResync(TournamentId TournamentId, long Sequence) : LiveNotification(TournamentId);

/// <summary>A sequenced event. Unknown types retain their name and JSON payload.</summary>
public sealed record LiveEvent(TournamentId TournamentId, long Sequence, string Type, JsonElement Data, object? Payload)
    : LiveNotification(TournamentId);

/// <summary>The Tournament's lifecycle changed.</summary>
public sealed record TournamentStatusChanged(TournamentStatus Status);
/// <summary>The Tournament completed with Final Placements.</summary>
public sealed record TournamentCompleted(TournamentStatus Status, IReadOnlyList<LiveFinalPlacement>? Placements);
/// <summary>A Final Placement range in a completion event; live events omit Player Identities.</summary>
public sealed record LiveFinalPlacement(ParticipantId ParticipantId, int From, int To);
/// <summary>Registration and Check-in counts changed.</summary>
public sealed record RegistrationsChanged(int Registered, int CheckedIn);
/// <summary>A Participant's lifecycle changed.</summary>
public sealed record ParticipantChanged(ParticipantId ParticipantId, ParticipantStatus Status);
/// <summary>A Stage began.</summary>
public sealed record StageStarted(StageId StageId, int Position);
/// <summary>A Stage completed.</summary>
public sealed record StageCompleted(StageId StageId, int Position);
/// <summary>A Match changed. Endpoint fields are present only when disclosed by the server.</summary>
public sealed record MatchChanged(MatchId MatchId, StageId StageId, GroupId GroupId, string Key, int Round,
    MatchStatus Status, IReadOnlyList<ParticipantId>? Participants, bool ServerAllocated, int Aborts,
    bool Aborted = false, MatchResult? Result = null, ParticipantId? WinnerId = null,
    string? ServerAddress = null, int? ServerPort = null);
/// <summary>Recorded Bouts, including any preserved after an Abort.</summary>
public sealed record BoutRecorded(MatchId MatchId, IReadOnlyList<Bout>? Bouts);
/// <summary>A Group's Standings changed.</summary>
public sealed record StandingsChanged(GroupId GroupId, bool Complete, IReadOnlyList<Standing>? Standings);

/// <summary>A live protocol failure, including its server problem code.</summary>
public sealed class LiveProtocolException(string code, string message) : Exception(message)
{
    /// <summary>The server's problem code.</summary>
    public string Code { get; } = code;
}
