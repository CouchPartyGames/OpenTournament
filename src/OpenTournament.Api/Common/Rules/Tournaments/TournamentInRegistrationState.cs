using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Common.Rules.Tournaments;

public sealed class TournamentInRegistrationState(Status status) : IRule
{
    public RuleError Error { get; } = new RuleError(nameof(TournamentInRegistrationState),
            "Tournament is not in a registration state", 
            "tournament.status");

    public bool Evaluate() => status == Status.Registration;
}
