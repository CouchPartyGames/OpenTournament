using JetBrains.Annotations;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Tests.Unit.Data.Models;

[TestSubject(typeof(Participant))]
public class ParticipantTest
{

    [Fact]
    public void CreateBye_ShouldReturnParticipant()
    {
            // Arrange
        var expected = new Participant()
        {
            Id = new ParticipantId("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
            Name = "Bye",
            Rank = Int32.MaxValue
        };
        
            // Act
        var actual = Participant.CreateBye();

            // Assert
        actual.Should().BeEquivalentTo(expected);
    }
}