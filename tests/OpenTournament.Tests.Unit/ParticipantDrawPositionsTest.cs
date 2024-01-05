namespace OpenTournament.Tests.Unit;

using OpenTournament.Common.Draw.Layout;

public class ParticipantDrawPositionsTest
{
    [Theory]
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

    [Fact]
    public void GetDrawSize2_ShouldReturnVersusMatches_WhenDrawSizeIs2()
    {
        // Arrange
        var expectedMatchCount = 1;
        var expected = new List<VersusMatch>() {
            new VersusMatch(1, 2)
        };
        
        // Act
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size2);
        var positions = new ParticipantPositions(drawSize);
        
        // Assert
        positions.Matches.Should().BeEquivalentTo(expected);
        
        positions.Matches.Should()
            .NotBeEmpty()
            .And.HaveCount(expectedMatchCount)
            .And.ContainItemsAssignableTo<VersusMatch>();
    }
    
    [Fact]
    public void GetDrawSize4_ShouldReturnVersusMatches_WhenDrawSizeIs4()
    {
        // Arrange
        var expectedMatchCount = 2;
        var expected = new List<VersusMatch>() {
            new VersusMatch(1, 4),
            new VersusMatch(3, 2)
        };
        
        // Act
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size4);
        var positions = new ParticipantPositions(drawSize);
        
        // Assert
        positions.Matches.Should().BeEquivalentTo(expected);
        
        positions.Matches.Should()
            .NotBeEmpty()
            .And.HaveCount(expectedMatchCount)
            .And.ContainItemsAssignableTo<VersusMatch>();
    }
    
    [Fact]
    public void GetDrawSize8_ShouldReturnVersusMatches_WhenDrawSizeIs8()
    {
        // Arrange
        var expectedMatchCount = 4;
        var expected = new List<VersusMatch>() {
            new VersusMatch(1, 8),
            new VersusMatch(6, 3),
            new VersusMatch(4, 5),
            new VersusMatch(7, 2)
        };
        
        // Act
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size8);
        var positions = new ParticipantPositions(drawSize);
        
        // Assert
        positions.Matches.Should().BeEquivalentTo(expected);
        
        positions.Matches.Should()
            .NotBeEmpty()
            .And.HaveCount(expectedMatchCount)
            .And.ContainItemsAssignableTo<VersusMatch>();
    }
}