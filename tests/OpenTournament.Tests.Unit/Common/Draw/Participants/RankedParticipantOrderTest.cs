using JetBrains.Annotations;
using NSubstitute.ExceptionExtensions;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Tests.Unit.Common.Draw.Participants;

[TestSubject(typeof(RankedParticipantOrder))]
public class RankedParticipantOrderTest
{
    
    [Fact]
    public void Opponents_ShouldReturnDictionary() {
        List<Participant> participants = new() {
            new Participant { Id = new ParticipantId("23"), Name = "Bob", Rank = 3},
            new Participant { Id = new ParticipantId("f"), Name = "Bill", Rank = 8},
            new Participant { Id = new ParticipantId("2g"), Name = "Billy", Rank = 7},
            new Participant { Id = new ParticipantId("23d"), Name = "B", Rank = 1},
            new Participant { Id = new ParticipantId("3"), Name = "B2", Rank = 2},
        };

        var expected = new Dictionary<int, Participant>()
        {
            { 1, new Participant { Id = new ParticipantId("f"), Name = "Bill", Rank = 8 } },
            { 2, new Participant { Id = new ParticipantId("2g"), Name = "Billy", Rank = 7 } },
            { 3, new Participant { Id = new ParticipantId("23"), Name = "Bob", Rank = 3 } },
            { 4, new Participant { Id = new ParticipantId("3"), Name = "B2", Rank = 2 } },
            { 5, new Participant { Id = new ParticipantId("23d"), Name = "B", Rank = 1 } },
        };

        var actual = new RankedParticipantOrder(participants).Opponents;
        
            // Assert
        actual.Should().BeEquivalentTo(expected);
    } 

    [Fact]
    public void Opponents_ShouldThrowException_WhenLessThan2Opponents()
    {
        List<Participant> participants = new();
        Action act = () =>
        {
             var opps = new RankedParticipantOrder(participants).Opponents;
        };

            // Assert
        act.Should().Throw<EmptyListOfOpponentsException>();
    }
}