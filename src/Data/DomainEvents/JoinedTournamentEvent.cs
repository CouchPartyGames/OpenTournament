using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed record JoinedTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : INotification, IDomainEvent;