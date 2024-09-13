using OpenTournament.Data.Models;
using MassTransit;

namespace OpenTournament.Data.DomainEvents;

public record MatchCompletedEvent(MatchId MatchId, TournamentId TournamentId) : IDomainEvent;

// <summary>
// Event signaling a Match has completed 
// </summary>
[EntityName("MatchCompleted")]
public record MatchCompleted {
    public required TournamentId TournamentId { get; init; }
    public required MatchId MatchId { get; init; }
}


// <summary>
// Event signaling a Match is ready (all opponents have completed their previous match)
// </summary>
public record MatchReadied {
    public required TournamentId tournamentId { get; init; }
    public required MatchId MatchId { get; init; }
}

// <summary>
//
// </summary>
public record MatchInProgress(MatchId MatchId);

public record MatchForfeited(MatchId MatchId, string Reason);
