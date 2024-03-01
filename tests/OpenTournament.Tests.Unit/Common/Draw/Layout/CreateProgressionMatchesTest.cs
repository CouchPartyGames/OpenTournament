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
                new List<CreateProgressionMatches.ProgressionMatch>
                {
                    new(1, 1, -1, 1, 2)
                }
            },
            new object[]
            {
                4,
                new List<CreateProgressionMatches.ProgressionMatch>
                {
                    new(1, 1, 3, 1, 4),
                    new(1, 2, 3, 3, 2),
                    new(2, 3, -1, -1, -1)
                }
            },
            new object[]
            {
                8,
                new List<CreateProgressionMatches.ProgressionMatch>
                {
                    new(1, 1, 5, 1, 8),
                    new(1, 2, 5, 6, 3),
                    new(1, 3, 6, 4, 5),
                    new(1, 4, 6, 7, 2),
                    
                    new(2, 5, 7, -1, -1),
                    new(2, 6, 7, -1, -1),
                    
                    new(3, 7, -1, -1, -1)
                }
            },
        };

    [Theory]
    [MemberData(nameof(testData))]
    public void MatchWithProgressions_Should_When(int numParticipants, 
        List<CreateProgressionMatches.ProgressionMatch> expected)
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