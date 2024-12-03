using JetBrains.Annotations;
using OpenTournament.Api.Common.Rules.Tournaments;

namespace OpenTournament.Tests.Unit.Common.Rules.Tournaments;

[TestSubject(typeof(TournamentHasMinimumParticipants))]
public class TournamentHasMinimumParticipantsTest
{

    [Theory]
    [InlineData(2,2)]
    [InlineData(5,2)]
    public void Constructor_ShouldEvaluateAsTrue_WhenMoreParticipantsThenMimimum(int num, int min)
    {
        var test = new TournamentHasMinimumParticipants(num, min);
        var result = test.Evaluate();

        result.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldReturnFalse_WhenMimimumParticipantsNotMet()
    {
        int num = 3;
        int min = 4;
        var test = new TournamentHasMinimumParticipants(num, min);
        var result = test.Evaluate();

        result.Should().BeFalse();
    }
}