using JetBrains.Annotations;
using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Tests.Unit.Common.Draw.Layout;

[TestSubject(typeof(CreateProgressionMatches))]
public class CreateProgressionMatchesTest
{
    
    public static List<object[]> testData =>
        new List<object[]>
        {
            new object[]
            {
                2,
                new List<CreateProgressionMatches.SingleEliminationMatch>
                {
                    new(1, 1, -1, 1, Position2:2)
                }
            },
            new object[]
            {
                4,
                new List<CreateProgressionMatches.SingleEliminationMatch>
                {
                    new(1, 1, 3, 1, 4),
                    new(1, 2, 3, 3, 2),
                    new(2, 3, -1, -1, -1)
                }
            },
        };

    [Theory]
    [MemberData(nameof(testData))]
    public void MatchWithProgressions_Should_When(int numParticipants, 
        List<CreateProgressionMatches.SingleEliminationMatch> expected)
    {
        // Arrange
        var positions = new FirstRoundPositions(DrawSize.CreateFromParticipants(numParticipants));
        var matchIds = new CreateMatchIds(positions).MatchByIds;

        // Act
        var actual = new CreateProgressionMatches(matchIds).MatchWithProgressions;

        // Assert 
        actual.Should().BeEquivalentTo(expected);
        
    }
}