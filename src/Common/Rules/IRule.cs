using Microsoft.IdentityModel.Tokens;

namespace OpenTournament.Common.Rules;

public sealed record RuleError(string Name, string Message, string Field);

public sealed record RuleFailure(List<RuleError> Errors);

public interface IRule
{
    public bool Evaluate();
    public RuleError Error { get;  }
}

public sealed class RuleEngine
{
    private List<IRule> _rules = new();

    public List<RuleError> Errors { get; } = new();

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

    public Dictionary<string, object> ToProblemExtensions()
    {
        Dictionary<string, object> errors = new();
        foreach (var error in Errors)
        {
           errors.Add(error.Field, $"{error.Message}");
        }

        return errors;
    }
}

