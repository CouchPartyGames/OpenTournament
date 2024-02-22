namespace OpenTournament.Tests.Unit.Common.Draw.Layout;

using OpenTournament.Common.Draw.Layout;

public class DrawMatchTest
{
    [Fact]
    public void DrawMatch_ShouldSetIdAndRound_When2ArgsSet()
    {
        // Act
        var drawMatch = new DrawMatch(3, 4);

        // Arrange
        drawMatch.Id.Should().Be(3);
        drawMatch.Round.Should().Be(4);
    }

    [Fact]
    public void DrawMatch2_ShouldSetIdRoundAndProgression_When3ArgsSet()
    {
        // Act
        var drawMatch = new DrawMatch(8, 2, 6);

        // Arrange
        drawMatch.Id.Should().Be(8);
        drawMatch.Round.Should().Be(2);
        drawMatch.WinProgression.Should().Be(6);
    }
    
    [Fact]
    public void DrawMatch3_ShouldSetIdRoundProgressionOpponents_When4ArgsSet()
    {
        // Act
        var drawMatch = new DrawMatch(7, 3, 3, 4);

        // Arrange
        drawMatch.Id.Should().Be(7);
        drawMatch.Round.Should().Be(1);
        drawMatch.WinProgression.Should().Be(3);
        drawMatch.Opponent1.Should().Be(3);
        drawMatch.Opponent2.Should().Be(4);
        
    }
    
    [Fact]
    public void SetProgression_Should_When()
    {
        // Act
        var drawMatch = new DrawMatch(8, 2);
        drawMatch.SetProgression(6);

        // Arrange
        drawMatch.WinProgression.Should().Be(6);
    }
}