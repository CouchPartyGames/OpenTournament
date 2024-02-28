using JetBrains.Annotations;
using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Tests.Unit.Common.Draw.Layout;

[TestSubject(typeof(CreateMatchIds))]
public class CreateMatchIdsTest
{
    public static List<object[]> testData =>
        new List<object[]>
        {
            new object[]
            {
                2,
                new List<CreateMatchIds.MatchWithId>
                {
                    new(1, 1, 1, 2)
                }
            },
            new object[]
            {
                4,
                new List<CreateMatchIds.MatchWithId>
                {
                    new(1, 1),
                    new(1, 2),
                    new(2, 3)
                }
            }
        };

    [Theory]
    [MemberData(nameof(testData))]
    public void MatchByIds_ShouldMatch_WhenParticipantsSet(int numParticipants, List<CreateMatchIds.MatchWithId> matches)
    {
        // Arrange
        var positions = new FirstRoundPositions(DrawSize.CreateFromParticipants(numParticipants));
        
        // Act
        var actual = new CreateMatchIds(positions).MatchByIds;

        // Assert
        actual.Should().BeEquivalentTo(matches);
    }
}