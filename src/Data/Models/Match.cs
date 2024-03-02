using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Data.Models;

public sealed record MatchId(Guid Value)
{
    public static MatchId? TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new MatchId(guid);
    }

    public static MatchId NewMatchId() => new (Guid.NewGuid());
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
    
    public ParticipantId Participant1Id { get; set; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant1 { get; set; }
    
    public ParticipantId Participant2Id { get; set; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant2 { get; set; }
    
    public int WinMatchId { get; set; }
    
    public int LoseMatchId { get; set; }
    
    
    public TournamentId TournamentId { get; set; }

    
    public static Match Create(TournamentId tournamentId, SingleEliminationFirstRound.SingleMatch match)
    {
        return new()
        {
            Id = MatchId.NewMatchId(),
            LocalMatchId = match.MatchId,
            TournamentId = tournamentId,
            State = MatchState.Ready,
            Participant1Id = match.Opp1.Id,
            Participant2Id = match.Opp2.Id,
            WinMatchId = match.WinMatchId
        };
    }
}