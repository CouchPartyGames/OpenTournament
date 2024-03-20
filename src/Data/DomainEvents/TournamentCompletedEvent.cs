using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed record TournamentCompletedEvent(TournamentId TournamentId) : INotification, IDomainEvent;