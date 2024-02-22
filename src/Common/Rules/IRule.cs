namespace OpenTournament.Common.Rules;

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

