namespace OpenTournament.Api.Data.Models;

public class MatchResults
{
    public bool IsCompleted { get; set; }
    
    public ParticipantId WinnerId { get; set; }
    
    public Dictionary<ParticipantId, string> Results { get; set; }
}