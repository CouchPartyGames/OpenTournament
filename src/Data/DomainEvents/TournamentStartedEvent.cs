using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed record TournamentStartedEvent(TournamentId TournamentId) : INotification, IDomainEvent;