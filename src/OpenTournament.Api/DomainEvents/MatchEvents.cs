﻿using OpenTournament.Data.Models;
using MassTransit;

namespace OpenTournament.Data.DomainEvents;

public record MatchCompletedEvent(MatchId MatchId, TournamentId TournamentId) : IDomainEvent;

// <summary>
// Event signaling a Match has completed 
// </summary>
public record MatchCompleted {
    public required MatchId MatchId { get; init; }
    public required TournamentId TournamentId { get; init; }
}


// <summary>
// Event signaling a Match is ready (all opponents have completed their previous match)
// </summary>
public record MatchReadied {
    public required MatchId MatchId { get; init; }
    public required TournamentId TournamentId { get; init; }
}

// <summary>
//
// </summary>
public record MatchInProgress(MatchId MatchId);

public record MatchForfeited(MatchId MatchId, string Reason);