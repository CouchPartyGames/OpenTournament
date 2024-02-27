using JetBrains.Annotations;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Tests.Unit.Common.Draw.Participants;

[TestSubject(typeof(RandomParticipantOrder))]
public class RandomParticipantOrderTest
{

    
    [Fact(Skip = "Not sure how to test")]
    public void Opponents_ShouldReturnParticipants_WhenRandom()
    {
        Dictionary<int, Participant> expected = new();
        List<Participant> participants = new();
        var actual = new RandomParticipantOrder(participants).Opponents;

        actual.Should().NotEqual(expected);
    }
    
    [Fact]
    public void Opponents_ShouldThrowException_WhenLessThan2Opponents()
    {
        List<Participant> participants = new();
        Action act = () =>
        {
             var opps = new RandomParticipantOrder(participants).Opponents;
        };

        act.Should().Throw<EmptyListOfOpponentsException>();
    }
}