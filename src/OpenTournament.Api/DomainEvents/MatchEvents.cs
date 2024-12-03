using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.DomainEvents;

public record MatchCompletedEvent(MatchId MatchId, TournamentId TournamentId) : IDomainEvent;

// <summary>
// Event signaling a Match has completed 
// </summary>
public record MatchCompleted {
    public required MatchId MatchId { get; init; }
    public required TournamentId TournamentId { get; init; }

    public required ParticipantId WinnerId { get; init; }

    public required int CompletedLocalMatchId { get; init; } 
}


// <summary>
// Event signaling a Match is ready (all opponents have completed their previous match)
// </summary>
public record MatchReadied {
    public required MatchId MatchId { get; init; }
    public required TournamentId TournamentId { get; init; }
}

// <summary>
// Event signaling a Match is in progress
// </summary>
public record MatchInProgress {
    public required MatchId MatchId { get; init; }

    public required TournamentId TournamentId { get; init; }
}

public record MatchForfeited(MatchId MatchId, string Reason);
