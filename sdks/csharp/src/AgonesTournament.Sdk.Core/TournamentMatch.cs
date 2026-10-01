namespace AgonesTournament.Sdk.Core;

/// <summary>One contest within a Group, including recorded Bouts and progression state.</summary>
public record TournamentMatch
{
    /// <summary>How often its Game Server failed mid-play.</summary>
    public required int Aborts { get; init; }
    /// <summary>Recorded Bouts in this Match; the API may return null.</summary>
    public required IReadOnlyList<Bout>? Bouts { get; init; }
    /// <summary>The double-elimination bracket, when supplied.</summary>
    public string? Bracket { get; init; }
    /// <summary>When play completed; absent until completion.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>The Group containing this Match.</summary>
    public required GroupId GroupId { get; init; }
    /// <summary>The UUID of this domain object.</summary>
    public required MatchId Id { get; init; }
    /// <summary>Stable position of the Match in its Group, e.g. R2-M1.</summary>
    public required string Key { get; init; }
    /// <summary>The Participants in this response; the API may return null.</summary>
    public required IReadOnlyList<ParticipantId>? Participants { get; init; }
    /// <summary>When this Match became Ready; absent until it is Ready.</summary>
    public DateTimeOffset? ReadyAt { get; init; }
    /// <summary>The Match outcome; absent until the Match is decided.</summary>
    public MatchResult? Result { get; init; }
    /// <summary>When the Match becomes Stalled if it has no result; absent before it is Ready.</summary>
    public DateTimeOffset? ResultDeadline { get; init; }
    /// <summary>The one-based Round number within the Group.</summary>
    public required int Round { get; init; }
    /// <summary>Whether a Game Server is allocated; this does not imply its endpoint is disclosed.</summary>
    public required bool ServerAllocated { get; init; }
    /// <summary>When the Game Server reported play started; absent before that report.</summary>
    public DateTimeOffset? StartedAt { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required MatchStatus Status { get; init; }
    /// <summary>The Tournament containing this data.</summary>
    public required TournamentId TournamentId { get; init; }
    /// <summary>The winning Participant; absent for outcomes without a single winner.</summary>
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
