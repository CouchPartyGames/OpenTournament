namespace OpenTournament.Common.Rules;

using OpenTournament.Models;

public sealed record RuleError(string Name, string Message, string Field);

public sealed record RuleFailure(List<RuleError> Errors);

public interface IRule
{
    public bool Evaluate();
    public RuleError Error { get;  }
}

public class RuleEngine
{
    private List<IRule> _rules = new();

    public List<RuleError> Errors { get; private set; } = new();

    public void Add(IRule rule) => _rules.Add(rule);

    public bool Evaluate()
    {
        bool result = true;
        foreach (var rule in _rules)
        {
            if (!rule.Evaluate())
            {
                result = false;
                Errors.Add(rule.Error);
            }
        }

        return result;
    }
}

public sealed class TournamentInRegistrationState : IRule
{
    public RuleError Error { get; private set; }

    private readonly Status _status;
    public TournamentInRegistrationState(Status status)
    {
        _status = status;
        Error = new RuleError(nameof(TournamentInRegistrationState),
            "Tournament is not in a registration state", "tournament.status");
    }
    
    public bool Evaluate() => _status == Status.Registration;
}
