namespace OpenTournament.Common.Rules.Tournaments;

public sealed class TournamentHasMinimumParticipants : IRule
{
    private readonly int _numParticipants;
    private readonly int _minParticipants;

    public TournamentHasMinimumParticipants(int numParticipants, int minParticipants)
    {
        _numParticipants = numParticipants;
        _minParticipants = minParticipants;
        Error = new RuleError(nameof(TournamentHasMinimumParticipants),
            $"Tournament hasn't meet mimimum number of participants ({_numParticipants} Required: {_minParticipants})",
            "tournament.minParticipants");
    }

    public bool Evaluate() => _numParticipants >= _minParticipants;
    public RuleError Error { get; }
}