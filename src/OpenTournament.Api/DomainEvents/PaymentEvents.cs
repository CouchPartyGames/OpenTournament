using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.DomainEvents;

public record PaymentProcessed(TournamentId TournamentId);