using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Tests.Unit.Common.Draw.Layout;


public class LocalMatchIdsTest
{
    private readonly ITestOutputHelper _output;
    
    public LocalMatchIdsTest(ITestOutputHelper output)
    {
        _output = output;
    }

    public static List<object[]> DrawSizeData =>
        new List<object[]>
        {
            new object[]
            {
                DrawSize.Size.Size2,
                new Dictionary<int, List<int>>
                {
                    { 1, [ 1] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size4,
                new Dictionary<int, List<int>>()
                {
                    { 1, [ 1, 2 ] },
                    { 2, [ 3 ] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size8,
                new Dictionary<int, List<int>>()
                {
                    { 1, [ 1, 2, 3, 4 ] },
                    { 2, [ 5, 6] },
                    { 3, [ 7 ] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size16,
                new Dictionary<int, List<int>>()
                {
                    { 1, [ 1, 2, 3, 4, 5, 6, 7, 8 ] },
                    { 2, [ 9, 10, 11, 12 ] },
                    { 3, [ 13, 14 ] },
                    { 4, [ 15 ] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size32,
                new Dictionary<int, List<int>>()
                {
                    { 1, Enumerable.Range(1, 16).ToList() },
                    { 2, Enumerable.Range(17, 8).ToList() },
                    { 3, Enumerable.Range(25, 4).ToList() },
                    { 4, [ 29, 30] },
                    { 5, [ 31 ] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size64,
                new Dictionary<int, List<int>>()
                {
                    { 1, Enumerable.Range(1, 32).ToList() },
                    { 2, Enumerable.Range(33, 16).ToList() },
                    { 3, Enumerable.Range(49, 8).ToList() },
                    { 4, [ 57, 58, 59, 60] },
                    { 5, [ 61, 62] },
                    { 6, [ 63 ] }
                }
            },
            new object[]
            {
                DrawSize.Size.Size128,
                new Dictionary<int, List<int>>()
                {
                    { 1, Enumerable.Range(1, 64).ToList() },
                    { 2, Enumerable.Range(65, 32).ToList() },
                    { 3, Enumerable.Range(97, 16).ToList() },
                    { 4, Enumerable.Range(113, 8).ToList() },
                    { 5, [ 121, 122, 123, 124 ] },
                    { 6, [ 125, 126 ] },
                    { 7, [ 127 ] }
                }
            }
        };

    [Theory(Skip = "No longer used")]
    [MemberData(nameof(DrawSizeData))]
    public void CreateMatchIds_ShouldReturnDictionary_WhenDrawSizeCorrect(DrawSize.Size size, 
        Dictionary<int, List<int>> expected)
    {
       /* DrawSize drawSize = DrawSize.Create(size);
        
        // Act
        var ids = new LocalMatchIds(drawSize);
        var actual = ids.CreateMatchIds();

        // Assert
        actual.Should().BeEquivalentTo(expected);*/
    }
}