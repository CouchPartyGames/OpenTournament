using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Models;

public sealed record MatchId(Guid Value)
{
    public static MatchId TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new MatchId(guid);
    }

    public static MatchId Create() => new MatchId(Guid.NewGuid());
}

public enum MatchState
{
    Ready = 0,
    InProgress,
    Complete
};

public sealed class Match
{
    [Column(TypeName = "varchar(36)")]
    public MatchId Id { get; set; }

    public MatchState State { get; set; } = MatchState.Ready;
    
    public int LocalMatchId { get; set; }
    
    //public ParticipantId Opponent1 { get; set; }
    
    //public ParticipantId Opponent2 { get; set; }
    //Progression WinProgression;
    //Progression LoseProgression;
}