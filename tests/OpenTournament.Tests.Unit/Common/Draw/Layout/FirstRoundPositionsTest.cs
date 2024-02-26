namespace OpenTournament.Tests.Unit.Common.Draw.Layout;

using OpenTournament.Common.Draw.Layout;

public class FirstRoundPositionsTest
{
    public static List<object[]> positions =>
        new List<object[]>
        {
            new object[]
            {
                2,
                new List<VersusMatch> {
                    new(1, 2)
                }
            },
            new object[]
            {
                4,
                new List<VersusMatch> {
                    new(1, 4),
                    new(3, 2)
                }
            },
            new object[]
            {
                8,
                new List<VersusMatch> {
                    new(1, 8),
                    new(6, 3),
                    new(4, 5),
                    new(7, 2)
                }
            }
        };
    
    [Theory]
    [InlineData(DrawSize.Size.Size2, 1)]
    [InlineData(DrawSize.Size.Size4, 2)]
    [InlineData(DrawSize.Size.Size8, 4)]
    [InlineData(DrawSize.Size.Size16, 8)]
    [InlineData(DrawSize.Size.Size32, 16)]
    [InlineData(DrawSize.Size.Size64, 32)]
    [InlineData(DrawSize.Size.Size128, 64)]
    public void GetDrawSize_ShouldMatch_WhenDrawSize(DrawSize.Size size, int expectedMatchCount)
    {
        // Arrange
        DrawSize drawSize = DrawSize.Create(size);
        
        // Act
        var positions = new FirstRoundPositions(drawSize);
        
        // Assert
        positions.Matches.Should()
            .NotBeEmpty()
            .And.HaveCount(expectedMatchCount)
            .And.ContainItemsAssignableTo<VersusMatch>();
    }
    

    [Theory]
    [MemberData(nameof(positions))]
    public void Constructor_ShouldSetMatches_WhenDrawSizeSet(int numParticipants, List<VersusMatch> expectedMatches)
    {
        // Act
        DrawSize drawSize = DrawSize.CreateFromParticipants(numParticipants);
        var positions = new FirstRoundPositions(drawSize);
        var matches = positions.Matches;

        // Assert
        positions.Matches.Should().BeEquivalentTo(expectedMatches);
    }
}