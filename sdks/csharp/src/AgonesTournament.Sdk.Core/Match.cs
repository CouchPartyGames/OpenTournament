using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The rules a Stage uses to pair Participants and determine advancement.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchFormat>))]
public enum MatchFormat
{
    /// <summary>One loss eliminates a Participant.</summary>
    [JsonStringEnumMemberName("single-elimination")] SingleElimination,
    /// <summary>Two losses eliminate a Participant.</summary>
    [JsonStringEnumMemberName("double-elimination")] DoubleElimination,
    /// <summary>Every Participant plays the other Participants.</summary>
    [JsonStringEnumMemberName("round-robin")] RoundRobin,
    /// <summary>Participants are paired by current Standing.</summary>
    [JsonStringEnumMemberName("swiss")] Swiss,
    /// <summary>Many Participants compete for placements and points.</summary>
    [JsonStringEnumMemberName("free-for-all")] FreeForAll
}

/// <summary>The current lifecycle state of a Match.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))]
public enum MatchStatus
{
    /// <summary>Waiting for opponent slots to fill.</summary>
    [JsonStringEnumMemberName("pending")] Pending,
    /// <summary>Ready to request a Game Server allocation.</summary>
    [JsonStringEnumMemberName("ready")] Ready,
    /// <summary>A Game Server allocation is being requested or confirmed.</summary>
    [JsonStringEnumMemberName("allocating")] Allocating,
    /// <summary>The Game Server has reported that play started.</summary>
    [JsonStringEnumMemberName("in-progress")] InProgress,
    /// <summary>No complete result arrived by the Result Deadline.</summary>
    [JsonStringEnumMemberName("stalled")] Stalled,
    /// <summary>The Match has been decided.</summary>
    [JsonStringEnumMemberName("completed")] Completed,
    /// <summary>The Match has been cancelled.</summary>
    [JsonStringEnumMemberName("cancelled")] Cancelled
}

/// <summary>The assigned Match, including completed Bouts to resume after an Abort.</summary>
public sealed record Match
{
    /// <summary>The allocated Match.</summary>
    public required MatchId MatchId { get; init; }
    /// <summary>The Tournament containing the Match.</summary>
    public required TournamentId TournamentId { get; init; }
    /// <summary>The Game catalog identifier.</summary>
    public required string GameId { get; init; }
    /// <summary>The current Match lifecycle state.</summary>
    public required MatchStatus Status { get; init; }
    /// <summary>The Stage Format used for this Match.</summary>
    public required MatchFormat Format { get; init; }
    /// <summary>Head-to-head only: the maximum number of Bouts (1 or 3).</summary>
    public int? BestOf { get; init; }
    /// <summary>Free-for-all only: the number of Bouts.</summary>
    public int? Bouts { get; init; }
    /// <summary>Participants with their Player Identities; the API may return null for an empty collection.</summary>
    public required IReadOnlyList<Participant>? Participants { get; init; }
    /// <summary>Recorded Bouts preserved after an Abort; the API may return null when none are complete.</summary>
    public required IReadOnlyList<Bout>? CompletedBouts { get; init; }
}

/// <summary>A Participant's Player Identity and whether they have forfeited.</summary>
public sealed record Participant
{
    /// <summary>The Participant this entry belongs to.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>The Player Identity provider or scheme, such as steam.</summary>
    public required string IdentityKind { get; init; }
    /// <summary>The external Player Identity value for the Participant.</summary>
    public required string IdentityValue { get; init; }
    /// <summary>Whether the Participant withdrew or was disqualified; do not wait for them or keep them connected.</summary>
    public required bool Forfeited { get; init; }
}

/// <summary>One completed play session within a Match.</summary>
public sealed record Bout
{
    /// <summary>The one-based Bout number.</summary>
    [JsonPropertyName("bout")]
    public required int Number { get; init; }
    /// <summary>Recorded results for the Bout; the API may return null for an empty collection.</summary>
    public required IReadOnlyList<BoutResult>? Results { get; init; }
}

/// <summary>A Participant's recorded result; omitted result flags and scores default to zero.</summary>
public sealed record BoutResult
{
    /// <summary>The Participant this entry belongs to.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>Whether the Participant won this head-to-head Bout.</summary>
    public bool Won { get; init; }
    /// <summary>The free-for-all placement; zero when omitted.</summary>
    public int Placement { get; init; }
    /// <summary>The free-for-all points; zero when omitted.</summary>
    public int Points { get; init; }
    /// <summary>Whether this individual Bout result was forfeited, including a No-show.</summary>
    public bool Forfeited { get; init; }
}

/// <summary>A Participant's free-for-all placement and points, computed by the Game Server.</summary>
/// <param name="ParticipantId">The Participant who placed.</param>
/// <param name="Placement">A distinct one-based placement within the Match.</param>
/// <param name="Points">Points computed by the Game Server; negative points are allowed.</param>
public sealed record BoutPlacement(
    [property: JsonRequired] ParticipantId ParticipantId,
    [property: JsonRequired] int Placement,
    [property: JsonRequired] int Points);
