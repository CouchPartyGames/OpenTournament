namespace AgonesTournament.Sdk.Core;

/// <summary>One contest within a Group, including recorded Bouts and progression state.</summary>
public record TournamentMatch
{
    /// <summary>How often its Game Server failed mid-play.</summary>
    public required int Aborts { get; init; }
    /// <summary>Bouts.</summary>
    public required IReadOnlyList<Bout>? Bouts { get; init; }
    /// <summary>Bracket.</summary>
    public string? Bracket { get; init; }
    /// <summary>CompletedAt.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>GroupId.</summary>
    public required GroupId GroupId { get; init; }
    /// <summary>Id.</summary>
    public required MatchId Id { get; init; }
    /// <summary>Stable position of the Match in its Group, e.g. R2-M1.</summary>
    public required string Key { get; init; }
    /// <summary>Participants.</summary>
    public required IReadOnlyList<ParticipantId>? Participants { get; init; }
    /// <summary>ReadyAt.</summary>
    public DateTimeOffset? ReadyAt { get; init; }
    /// <summary>Result.</summary>
    public MatchResult? Result { get; init; }
    /// <summary>ResultDeadline.</summary>
    public DateTimeOffset? ResultDeadline { get; init; }
    /// <summary>Round.</summary>
    public required int Round { get; init; }
    /// <summary>ServerAllocated.</summary>
    public required bool ServerAllocated { get; init; }
    /// <summary>StartedAt.</summary>
    public DateTimeOffset? StartedAt { get; init; }
    /// <summary>Status.</summary>
    public required MatchStatus Status { get; init; }
    /// <summary>TournamentId.</summary>
    public required TournamentId TournamentId { get; init; }
    /// <summary>WinnerId.</summary>
    public ParticipantId? WinnerId { get; init; }
}

/// <summary>A Match read with Game Server endpoint fields disclosed only when authorized by the API.</summary>
public sealed record MatchDetails : TournamentMatch
{
    /// <summary>Only for the Match's Participants and the Organizer.</summary>
    public string? ServerAddress { get; init; }
    /// <summary>Only for the Match's Participants and the Organizer.</summary>
    public int? ServerPort { get; init; }
}
