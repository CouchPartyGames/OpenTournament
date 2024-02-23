using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Tests.Unit.Common.Draw.Participants;

[TestSubject(typeof(ParticipantOrder))]
public class ParticipantOrderTest
{

    [Fact]
    public void Create_ShouldReturnRandomOrder_WhenOrderIsRandom()
    {
        var result = ParticipantOrder.Create(ParticipantOrder.Order.Random, 
            new List<Participant>());
        
        result.Should().BeOfType<RandomParticipantOrder>();
    }
    
    [Fact]
    public void Create_ShouldReturnRankedOrder_WhenOrderIsRanked()
    {
        var result = ParticipantOrder.Create(ParticipantOrder.Order.Ranked, 
            new List<Participant>());

        result.Should().BeOfType<RankedParticipantOrder>();
    }
}