using JetBrains.Annotations;
using OpenTournament.Common.Rules;

namespace OpenTournament.Tests.Unit.Common.Rules;

[TestSubject(typeof(RuleEngine))]
public class RuleEngineTest
{

    [Fact]
    public void Evaluate_ShouldReturnTrue_WhenAllRulesPass()
    {
            // Arrange
        var rule = Substitute.For<IRule>();
        rule.Evaluate().Returns(true);
        
        var engine = new RuleEngine();
        engine.Add(rule);


            // Act
        var result = engine.Evaluate();

            // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldReturnFalse_WhenRuleFails()
    {
        var rule1 = Substitute.For<IRule>();
        rule1.Evaluate().Returns(true);
        
        var rule2 = Substitute.For<IRule>();
        rule2.Evaluate().Returns(true);
        
        var rule3 = Substitute.For<IRule>();
        rule3.Evaluate().Returns(false);
        
        var rule4 = Substitute.For<IRule>();
        rule4.Evaluate().Returns(true);
        
            // Arrange
        var engine = new RuleEngine();
        engine.Add(rule1);
        engine.Add(rule2);
        engine.Add(rule3);
        engine.Add(rule4);


            // Act
        var result = engine.Evaluate();

            // Assert
        result.Should().BeFalse();
    }
}