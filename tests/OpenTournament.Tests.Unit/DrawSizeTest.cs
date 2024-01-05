namespace OpenTournament.Tests.Unit;

using OpenTournament.Common.Draw.Layout;

public class DrawSizeTest
{
    [Theory]
    [InlineData(DrawSize.Size.Size2)]
    [InlineData(DrawSize.Size.Size4)]
    [InlineData(DrawSize.Size.Size8)]
    [InlineData(DrawSize.Size.Size16)]
    [InlineData(DrawSize.Size.Size32)]
    [InlineData(DrawSize.Size.Size64)]
    [InlineData(DrawSize.Size.Size128)]
    public void Create_ShouldCreate_WhenEnumSizeSet(DrawSize.Size input)
    {
        // Arrange
        // Act
		DrawSize actualDrawSize = DrawSize.Create(input);
        
        // Assert
        actualDrawSize.Value.Should().Be(input);
    }
    
    [Theory]
    [InlineData(2, 2)]
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
    
    [Fact(Skip = "skip for now")]
    public void CreateFromParticipants_ShouldThrowException_WhenBadCastToEnum()
    {
        // Arrange
        var badData = (DrawSize.Size)3;
        
        // Act
        Action result = () =>
        {
            DrawSize bad = DrawSize.Create(badData);
        };

        // Assert
        result.Should().Throw<InvalidDrawSizeException>();
    }
}
