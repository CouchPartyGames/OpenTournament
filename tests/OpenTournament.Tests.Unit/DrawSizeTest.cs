namespace OpenTournament.Tests.Unit;

using OpenTournament.Common.Draw.Layout;

public class DrawSizeTest
{
    [Theory]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    public void Create_ShouldCreate_WhenEnumSizeSet(int size, int expected)
    {
        // Arrange
        DrawSize.Size actualEnum = (DrawSize.Size)size;
        DrawSize.Size expectedEnum = (DrawSize.Size)expected;
        
        // Act
		DrawSize actualDrawSize = DrawSize.Create(actualEnum);
        
        // Assert
        actualDrawSize.Value.Should().Be(expectedEnum);
    }
    
    [Theory]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(12, 16)]
    [InlineData(32, 32)]
    [InlineData(33, 64)]
    [InlineData(53, 64)]
    [InlineData(67, 128)]
    [InlineData(127, 128)]
    public void CreateFromParticipants_ShouldCreateDrawSize_WhenParticipantsSet(int participants, int expected)
    {
        // Arrange
        // Act
        DrawSize size = DrawSize.CreateFromParticipants(participants);
        
        // Assert
        var expectedEnum = (DrawSize.Size)expected;
        Assert.Equal( expectedEnum, size.Value);
    }

    [Fact]
    public void CreateFromParticipants_ShouldThrowException_WhenBadParticipants()
    {
        Action result = () =>
        {
            DrawSize.CreateFromParticipants(999);
        };
        
        // Assert
        result.Should().Throw<InvalidDrawSizeException>();
    }
}
