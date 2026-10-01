using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The rules a Stage uses to pair Participants and determine advancement.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchFormat>))]
public enum MatchFormat
{
    [JsonStringEnumMemberName("single-elimination")] SingleElimination,
    [JsonStringEnumMemberName("double-elimination")] DoubleElimination,
    [JsonStringEnumMemberName("round-robin")] RoundRobin,
    [JsonStringEnumMemberName("swiss")] Swiss,
    [JsonStringEnumMemberName("free-for-all")] FreeForAll
}

/// <summary>The current lifecycle state of a Match.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))]
public enum MatchStatus
{
    [JsonStringEnumMemberName("pending")] Pending,
    [JsonStringEnumMemberName("ready")] Ready,
    [JsonStringEnumMemberName("allocating")] Allocating,
    [JsonStringEnumMemberName("in-progress")] InProgress,
    [JsonStringEnumMemberName("stalled")] Stalled,
    [JsonStringEnumMemberName("completed")] Completed,
    [JsonStringEnumMemberName("cancelled")] Cancelled
}

/// <summary>The assigned Match, including completed Bouts to resume after an Abort.</summary>
public sealed record Match
{
    public required MatchId MatchId { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string GameId { get; init; }
    public required MatchStatus Status { get; init; }
    public required MatchFormat Format { get; init; }
    /// <summary>Head-to-head only: the maximum number of Bouts (1 or 3).</summary>
    public int? BestOf { get; init; }
    /// <summary>Free-for-all only: the number of Bouts.</summary>
    public int? Bouts { get; init; }
    public required IReadOnlyList<Participant>? Participants { get; init; }
    public required IReadOnlyList<Bout>? CompletedBouts { get; init; }
}

/// <summary>A Participant's Player Identity and whether they have forfeited.</summary>
public sealed record Participant
{
    public required ParticipantId ParticipantId { get; init; }
    public required string IdentityKind { get; init; }
    public required string IdentityValue { get; init; }
    public required bool Forfeited { get; init; }
}

/// <summary>One completed play session within a Match.</summary>
public sealed record Bout
{
    [JsonPropertyName("bout")]
    public required int Number { get; init; }
    public required IReadOnlyList<BoutResult>? Results { get; init; }
}

/// <summary>A Participant's recorded result; omitted result flags and scores default to zero.</summary>
public sealed record BoutResult
{
    public required ParticipantId ParticipantId { get; init; }
    public bool Won { get; init; }
    public int Placement { get; init; }
    public int Points { get; init; }
    public bool Forfeited { get; init; }
}

/// <summary>A Participant's free-for-all placement and points, computed by the Game Server.</summary>
public sealed record BoutPlacement(
    [property: JsonRequired] ParticipantId ParticipantId,
    [property: JsonRequired] int Placement,
    [property: JsonRequired] int Points);
