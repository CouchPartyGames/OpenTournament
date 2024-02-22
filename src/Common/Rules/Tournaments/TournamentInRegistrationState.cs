namespace OpenTournament.Common.Rules.Tournaments;

using OpenTournament.Data.Models;

public sealed class TournamentInRegistrationState : IRule
{
    public RuleError Error { get; } = new RuleError(nameof(TournamentInRegistrationState),
            "Tournament is not in a registration state", 
            "tournament.status");

    private readonly Status _status;
    
    public TournamentInRegistrationState(Status status) => (_status) = (status);
    
    public bool Evaluate() => _status == Status.Registration;
}
