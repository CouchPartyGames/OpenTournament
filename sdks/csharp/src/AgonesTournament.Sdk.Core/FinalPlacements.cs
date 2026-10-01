namespace AgonesTournament.Sdk.Core;

/// <summary>The Final Placements and status of one Tournament.</summary>
public sealed record FinalPlacements
{
    /// <summary>Placements.</summary>
    public required IReadOnlyList<FinalPlacement>? Placements { get; init; }
    /// <summary>Status.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>TournamentId.</summary>
    public required TournamentId TournamentId { get; init; }
}

/// <summary>A Participant’s finishing position range in the whole Tournament.</summary>
public sealed record FinalPlacement
{
    /// <summary>Best place of the shared range.</summary>
    public required int From { get; init; }
    /// <summary>Identity.</summary>
    public required PlayerIdentity Identity { get; init; }
    /// <summary>ParticipantId.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>Worst place of the shared range.</summary>
    public required int To { get; init; }
}
