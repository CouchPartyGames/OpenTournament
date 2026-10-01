using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The runtime Stages of a Tournament and its current status.</summary>
public sealed record TournamentStructure
{
    /// <summary>The configured Stages in play order; the API may return null.</summary>
    public required IReadOnlyList<Stage>? Stages { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>The Tournament containing this data.</summary>
    public required TournamentId TournamentId { get; init; }
}

/// <summary>One phase of a Tournament played in one Format.</summary>
public sealed record Stage
{
    /// <summary>The rules used to pair Participants and determine advancement.</summary>
    public required MatchFormat Format { get; init; }
    /// <summary>The Groups playing this Stage independently; the API may return null.</summary>
    public required IReadOnlyList<Group>? Groups { get; init; }
    /// <summary>The UUID of this domain object.</summary>
    public required StageId Id { get; init; }
    /// <summary>The one-based position within the containing Tournament or Stage.</summary>
    public required int Position { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required StageStatus Status { get; init; }
}

/// <summary>A pool of Participants playing independently within a Stage.</summary>
public sealed record Group
{
    /// <summary>The UUID of this domain object.</summary>
    public required GroupId Id { get; init; }
    /// <summary>The Participants in this response; the API may return null.</summary>
    public required IReadOnlyList<GroupParticipant>? Participants { get; init; }
    /// <summary>The one-based position within the containing Tournament or Stage.</summary>
    public required int Position { get; init; }
    /// <summary>The Rounds played in this Group; the API may return null.</summary>
    public required IReadOnlyList<Round>? Rounds { get; init; }
    /// <summary>The current Standings in this Group; the API may return null.</summary>
    public required IReadOnlyList<Standing>? Standings { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required StageStatus Status { get; init; }
}

/// <summary>A Participant seeded into a Group and whether they advanced.</summary>
public sealed record GroupParticipant
{
    /// <summary>Whether this Participant advanced from the Group.</summary>
    public bool? Advanced { get; init; }
    /// <summary>The Participant this entry belongs to.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>The one-based Seeding position in the Group.</summary>
    public required int Seed { get; init; }
}

/// <summary>One step of a Stage containing a set of Matches.</summary>
public sealed record Round
{
    /// <summary>The double-elimination bracket, when supplied.</summary>
    public Bracket? Bracket { get; init; }
    /// <summary>The Matches played in this Round; the API may return null.</summary>
    public required IReadOnlyList<TournamentMatch>? Matches { get; init; }
    /// <summary>The one-based Round number within the Group.</summary>
    [JsonPropertyName("round")]
    public required int Number { get; init; }
}

/// <summary>A Participant’s position and results within a Group.</summary>
public sealed record Standing
{
    /// <summary>Free-for-all only.</summary>
    public long? BestPlacement { get; init; }
    /// <summary>Round robin only.</summary>
    public long? BoutDifferential { get; init; }
    /// <summary>Swiss only.</summary>
    public long? Buchholz { get; init; }
    /// <summary>Withdrew or was disqualified.</summary>
    public bool? Dropped { get; init; }
    /// <summary>Elimination formats only.</summary>
    public bool? Eliminated { get; init; }
    /// <summary>The number of Matches lost.</summary>
    public required long Losses { get; init; }
    /// <summary>The Participant this entry belongs to.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>The number of Matches played.</summary>
    public required long Played { get; init; }
    /// <summary>The accumulated points used for Standing.</summary>
    public required long Points { get; init; }
    /// <summary>1-based position in the Group.</summary>
    public required long Position { get; init; }
    /// <summary>The number of Matches won.</summary>
    public required long Wins { get; init; }
}
