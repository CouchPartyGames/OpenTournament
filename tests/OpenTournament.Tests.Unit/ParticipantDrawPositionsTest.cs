namespace OpenTournament.Tests.Unit;

using OpenTournament.Common.Draw.Layout;

public class ParticipantDrawPositionsTest
{
    [Theory]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 4)]
    [InlineData(16, 8)]
    [InlineData(32, 16)]
    [InlineData(64, 32)]
    [InlineData(128, 64)]
    public void GetDrawSize_ShouldMatch_WhenDrawSize(int size, int expectedMatchCount)
    {

        // Arrange
        DrawSize drawSize = DrawSize.Create((DrawSize.Size) size);
        
        // Act
        var positions = new ParticipantPositions(drawSize);
        
        // Assert
        positions.Matches.Should()
            .NotBeEmpty()
            .And.HaveCount(expectedMatchCount)
            .And.ContainItemsAssignableTo<VersusMatch>();
    }
}