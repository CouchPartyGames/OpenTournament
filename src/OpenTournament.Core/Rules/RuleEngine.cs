
namespace OpenTournament.Core.Rules;

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
           errors.Add(error.Field, $"{error.Name} - {error.Message}");
        }

        return errors;
    }

    
    public Dictionary<string, string[]> ToValidationExtensions()
    {
        Dictionary<string, string[]> errors = new();
        foreach (var error in Errors)
        {
           errors.Add(error.Field, [$"{error.Name} - {error.Message}"]);
        }
        return errors;
    }
}
