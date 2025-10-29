namespace OpenTournament.Core.Rules;

public sealed record RuleError(string Name, string Message, string Field);


public interface IRule
{
    public bool Evaluate();
    public RuleError Error { get;  }
}