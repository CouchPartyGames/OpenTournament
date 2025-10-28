using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Events;

public record PaymentProcessed(TournamentId TournamentId);