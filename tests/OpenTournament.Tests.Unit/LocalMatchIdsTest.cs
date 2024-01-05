using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Tests.Unit;

public class LocalMatchIdsTest
{
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize2()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, [ 1 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size2);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize4()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, [ 1, 2 ] },
            { 2, [ 3] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size4);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize8()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, [ 1, 2, 3, 4 ] },
            { 2, [ 5, 6] },
            { 3, [ 7 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size8);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize16()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, [ 1, 2, 3, 4, 5, 6, 7, 8 ] },
            { 2, [ 9, 10, 11, 12 ] },
            { 3, [ 13, 14 ] },
            { 4, [ 15 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size16);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
}