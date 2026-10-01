using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The runtime Stages of a Tournament and its current status.</summary>
public sealed record TournamentStructure
{
    /// <summary>Stages.</summary>
    public required IReadOnlyList<Stage>? Stages { get; init; }
    /// <summary>Status.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>TournamentId.</summary>
    public required TournamentId TournamentId { get; init; }
}

/// <summary>One phase of a Tournament played in one Format.</summary>
public sealed record Stage
{
    /// <summary>Format.</summary>
    public required MatchFormat Format { get; init; }
    /// <summary>Groups.</summary>
    public required IReadOnlyList<Group>? Groups { get; init; }
    /// <summary>Id.</summary>
    public required StageId Id { get; init; }
    /// <summary>Position.</summary>
    public required int Position { get; init; }
    /// <summary>Status.</summary>
    public required StageStatus Status { get; init; }
}

/// <summary>A pool of Participants playing independently within a Stage.</summary>
public sealed record Group
{
    /// <summary>Id.</summary>
    public required GroupId Id { get; init; }
    /// <summary>Participants.</summary>
    public required IReadOnlyList<GroupParticipant>? Participants { get; init; }
    /// <summary>Position.</summary>
    public required int Position { get; init; }
    /// <summary>Rounds.</summary>
    public required IReadOnlyList<Round>? Rounds { get; init; }
    /// <summary>Standings.</summary>
    public required IReadOnlyList<Standing>? Standings { get; init; }
    /// <summary>Status.</summary>
    public required StageStatus Status { get; init; }
}

/// <summary>A Participant seeded into a Group and whether they advanced.</summary>
public sealed record GroupParticipant
{
    /// <summary>Advanced.</summary>
    public bool? Advanced { get; init; }
    /// <summary>ParticipantId.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>Seed.</summary>
    public required int Seed { get; init; }
}

/// <summary>One step of a Stage containing a set of Matches.</summary>
public sealed record Round
{
    /// <summary>Bracket.</summary>
    public Bracket? Bracket { get; init; }
    /// <summary>Matches.</summary>
    public required IReadOnlyList<TournamentMatch>? Matches { get; init; }
    /// <summary>Round.</summary>
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
    /// <summary>Losses.</summary>
    public required long Losses { get; init; }
    /// <summary>ParticipantId.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>Played.</summary>
    public required long Played { get; init; }
    /// <summary>Points.</summary>
    public required long Points { get; init; }
    /// <summary>1-based position in the Group.</summary>
    public required long Position { get; init; }
    /// <summary>Wins.</summary>
    public required long Wins { get; init; }
}
