using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public class MatchResults
{
    public bool IsCompleted { get; set; }
    
    public ParticipantId WinnerId { get; set; }
    
    public Dictionary<ParticipantId, string> Results { get; set; }
}