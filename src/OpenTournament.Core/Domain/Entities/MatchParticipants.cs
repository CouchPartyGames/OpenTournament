using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public class MatchParticipants
{
    List<ParticipantId> Participants { get; set; }
}