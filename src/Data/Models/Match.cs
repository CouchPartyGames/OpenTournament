using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Data.Models;

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

public class Match
{
    [Column(TypeName = "varchar(36)")]
    public MatchId Id { get; set; }

    public MatchState State { get; set; } = MatchState.Ready;
    
    public int LocalMatchId { get; set; }
    
    
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant1 { get; set; }
    
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant2 { get; set; }
    
    //Progression WinProgression;
    //Progression LoseProgression;
    
    
    public TournamentId TournamentId { get; set; }
}