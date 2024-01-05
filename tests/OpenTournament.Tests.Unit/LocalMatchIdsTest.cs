using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Tests.Unit;

public class LocalMatchIdsTest
{
    private readonly ITestOutputHelper _output;
    
    public LocalMatchIdsTest(ITestOutputHelper output)
    {
        _output = output;
    }


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
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize32()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, Enumerable.Range(1, 16).ToList() },
            { 2, Enumerable.Range(17, 8).ToList() },
            { 3, Enumerable.Range(25, 4).ToList() },
            { 4, [ 29, 30] },
            { 5, [ 31 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size32);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize64()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, Enumerable.Range(1, 32).ToList() },
            { 2, Enumerable.Range(33, 16).ToList() },
            { 3, Enumerable.Range(49, 8).ToList() },
            { 4, [ 57, 58, 59, 60] },
            { 5, [ 61, 62] },
            { 6, [ 63 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size64);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSize128()
    {
        // Arrange
        var expected = new Dictionary<int, List<int>>()
        {
            { 1, Enumerable.Range(1, 64).ToList() },
            { 2, Enumerable.Range(65, 32).ToList() },
            { 3, Enumerable.Range(97, 16).ToList() },
            { 4, Enumerable.Range(113, 8).ToList() },
            { 5, [ 121, 122, 123, 124 ] },
            { 6, [ 125, 126 ] },
            { 7, [ 127 ] }
        };
        DrawSize drawSize = DrawSize.Create(DrawSize.Size.Size128);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
}