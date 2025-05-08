namespace OpenTournament.Api.Data.Models;

public class MatchMetadata
{
    public enum State
    {
        Waiting,
        Ready,
        InProgress,
        Completed
    }
    
    public MatchId MatchId { get; set; }
    
    public int LocalMatchId { get; set; }
    
    public MatchResults MatchResults { get; set; }
    
    public Dictionary<int, ParticipantId> MatchParticipants { get; set; }
    
    public MatchProgressions MatchProgressions { get; set; }

    public State MatchState { get; set; } = State.Waiting;
    
    public Dictionary<string, string> Metadata { get; set; }
}