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
    Wait = 0,
    Ready,
    InProgress,
    Complete
};

public sealed class Match
{
    [Column(TypeName = "varchar(36)")]
    public MatchId Id { get; init; }

    public MatchState State { get; private set; } = MatchState.Ready;
    
    public int LocalMatchId { get; init; }
    
    public ParticipantId Participant1Id { get; init; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant1 { get; init; }
    
    public ParticipantId? Participant2Id { get; private set; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant2 { get; init; }
    
    public int? WinMatchId { get; init; }
    
    public int LoseMatchId { get; init; }
    
    public ParticipantId? WinnerId { get; private set; }
    
    public TournamentId TournamentId { get; init; }
    
    public DateTime Created { get; init; }
    public DateTime Completed { get; private set; }

    
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

    public static Match CreateWithOneOpponent(TournamentId tournamentId, int localMatchId, int nextMatchId, ParticipantId participantId) {
        return new() {
            Id = MatchId.NewMatchId(),
            LocalMatchId = localMatchId,
            TournamentId = tournamentId,
            State = MatchState.Wait,
            Participant1Id = participantId,
            WinMatchId = nextMatchId
        };
    }

    public void UpdateOpponent(ParticipantId participantId) {
        State = MatchState.Ready;
        Participant2Id = participantId;
    }

    public void Complete(ParticipantId winnerId)
    {
        State = MatchState.Complete;
        WinnerId = winnerId;
        Completed = DateTime.Now;
    }
}