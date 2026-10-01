namespace AgonesTournament.Sdk.Core;

/// <summary>The Final Placements and status of one Tournament.</summary>
public sealed record FinalPlacements
{
    /// <summary>The Participants’ Final Placement ranges; the API may return null.</summary>
    public required IReadOnlyList<FinalPlacement>? Placements { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>The Tournament containing this data.</summary>
    public required TournamentId TournamentId { get; init; }
}

/// <summary>A Participant’s finishing position range in the whole Tournament.</summary>
public sealed record FinalPlacement
{
    /// <summary>Best place of the shared range.</summary>
    public required int From { get; init; }
    /// <summary>The Player Identity identifying this Participant.</summary>
    public required PlayerIdentity Identity { get; init; }
    /// <summary>The Participant this entry belongs to.</summary>
    public required ParticipantId ParticipantId { get; init; }
    /// <summary>Worst place of the shared range.</summary>
    public required int To { get; init; }
}
