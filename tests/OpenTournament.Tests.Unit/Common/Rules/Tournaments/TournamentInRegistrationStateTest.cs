using JetBrains.Annotations;
using OpenTournament.Api.Common.Rules.Tournaments;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Tests.Unit.Common.Rules.Tournaments;

[TestSubject(typeof(TournamentInRegistrationState))]
public class TournamentInRegistrationStateTest
{

    [Fact]
    public void Evaluate_ShouldReturnTrue_WhenStateInRegistration()
    {
        var rule = new TournamentInRegistrationState(Status.Registration);
        var result = rule.Evaluate();

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldReturnFalse_WhenStateNotInRegistration()
    {
        var rule = new TournamentInRegistrationState(Status.Completed);
        var result = rule.Evaluate();

        result.Should().BeFalse();
    }
}