using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed record LeftTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : INotification;