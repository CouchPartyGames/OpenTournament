using JetBrains.Annotations;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Tests.Unit.Common.Draw.Participants;

[TestSubject(typeof(RankedParticipantOrder))]
public class RankedParticipantOrderTest
{

    [Fact(Skip = "broken, need to fix")]
    public void Opponents_ShouldReturn_When()
    {
        List<Participant> participants = new();
        var expected = new Dictionary<OpponentOrder, Participant>();
        var actual = new RankedParticipantOrder(participants).Opponents;

        actual.Should().Equal(expected);
    }
}