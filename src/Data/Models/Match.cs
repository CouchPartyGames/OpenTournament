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
    public MatchId Id { get; init; }

    public MatchState State { get; private set; } = MatchState.Ready;
    
    public int LocalMatchId { get; init; }
    
    public ParticipantId Participant1Id { get; init; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant1 { get; init; }
    
    public ParticipantId Participant2Id { get; init; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant2 { get; init; }
    
    public int WinMatchId { get; init; }
    
    public int LoseMatchId { get; init; }
    
    
    public TournamentId TournamentId { get; init; }

    
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

    public void Complete()
    {
        State = MatchState.Complete;
    }
}