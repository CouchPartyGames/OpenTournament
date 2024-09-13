using OpenTournament.Common.Draw.Layout;
using OpenTournament.Data.Models;
using MassTransit;

namespace OpenTournament.Data.DomainEvents;

public enum StartType {
    Manual,
    Scheduled
}

public record TournamentStartedEvent(TournamentId TournamentId, DrawSize DrawSize) : IDomainEvent;

// <summary>
// Event signaling a Tournament has Started
// </summary>
[EntityName("TournamentStarted")]
public record TournamentStarted {
    public required TournamentId TournamentId { get; init; }

    public required DrawSize DrawSize { get; init; }

    public required StartType StartType { get; init; } 
}

public record TournamentCompletedEvent(TournamentId TournamentId) : IDomainEvent;
public record TournamentCompleted(TournamentId TournamentId);

// <summary>
// Tournament Created
// </summary>
[EntityName("TournamentCreated")]
public record TournamentCreated(TournamentId TournamentId);

public record TournamentUpdated(TournamentId TournamentId);

public record TournamentDrawFinalized(TournamentId TournamentId);

public record TournamentReady(TournamentId TournamentId);


// <summary>
// Tournament cancelled
// </summary>
public record TournamentCancelled(TournamentId TournamentId, string Reason);
