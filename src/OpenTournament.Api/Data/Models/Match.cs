using System.ComponentModel.DataAnnotations.Schema;
using LocalMatch = CouchPartyGames.TournamentGenerator.Type;

namespace OpenTournament.Api.Data.Models;

public sealed record MatchId(Guid Value)
{
    public static MatchId? TryParse(string id)
    {
        return !Guid.TryParse(id, out Guid guid) ? null : new MatchId(guid);
    }

    public static MatchId NewMatchId() => new (Guid.CreateVersion7());
}

public enum MatchState
{
    Wait = 0,
    Ready,
    InProgress,
    Complete
};

public sealed record Progression(int WinProgressionId, int LoseProgressionId)
{
    public const int NoProgression = -1;
    public static Progression NewWinLose(int win, int lose) => new(win, lose);
    public static Progression NewWin(int win) => new(win, NoProgression);
    public static Progression NewNoProgression() => new(NoProgression, NoProgression);
}

public sealed record Completion(ParticipantId WinnerId, DateTime CompletedOnUtc)
{
    public static Completion New(ParticipantId WinnerId) => new(WinnerId, DateTime.UtcNow);
}

//public record MatchComplete(ParticipantId WinnerId, MatchState State, DateTime CompletionDate);

public sealed class Match
{
        
    [Column(TypeName = "varchar(36)")]
    public required MatchId Id { get; init; }

    public MatchState State { get; private set; } = MatchState.Ready;
    
    public required int LocalMatchId { get; init; }
    
    public ParticipantId Participant1Id { get; init; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant1 { get; init; }
    
    public ParticipantId? Participant2Id { get; private set; }
    
    //[DeleteBehavior(DeleteBehavior.NoAction)]
    public Participant Participant2 { get; init; }
    
    // LocalMatchId
    public int? WinMatchId { get; init; } 

    public int LoseMatchId { get; init; } = Progression.NoProgression;
    
    public Progression Progression { get; init; }
    
    //public Completion Completion { get; private set; }
    
    public ParticipantId? WinnerId { get; private set; }
    
    public required TournamentId TournamentId { get; init; }
    
    public DateTime Created { get; init; }
    public DateTime Completed { get; private set; }


    public static Match NewUndetermined(TournamentId tournamentId, 
        LocalMatch.Match<Participant> localMatch,
        Progression progression)
    {
        return new Match
        {
            Id = MatchId.NewMatchId(),
            TournamentId = tournamentId,
            Participant1Id = localMatch.Opponent1.Id,
            Participant2Id = localMatch.Opponent2.Id,
            Created = DateTime.UtcNow,
            State = MatchState.Ready,
            Progression = progression,
            WinMatchId = localMatch.WinProgression,
            LoseMatchId = Progression.NoProgression,
            LocalMatchId = localMatch.LocalMatchId,
        };
    }

    public static Match NewCompleted(TournamentId tournamentId,
        LocalMatch.Match<Participant> localMatch,
        Progression progression, 
        Completion completion)
    {
        return new Match
        {
            Id = MatchId.NewMatchId(),
            TournamentId = tournamentId,
            Participant1Id = localMatch.Opponent1.Id,
            Participant2Id = localMatch.Opponent2.Id,
            Created = DateTime.UtcNow,
            State = MatchState.Complete,
            WinMatchId = localMatch.WinProgression,
            LoseMatchId = Progression.NoProgression,
            LocalMatchId = localMatch.LocalMatchId,
            Progression = progression,
            WinnerId = completion.WinnerId,
            //Completion = completion
        };
    }
    

    public static Match NewOneOpponent(TournamentId tournamentId, int localMatchId, Progression progression, ParticipantId participantId) {
        return new Match {
            Id = MatchId.NewMatchId(),
            LocalMatchId = localMatchId,
            TournamentId = tournamentId,
            State = MatchState.Wait,
            Participant1Id = participantId,
            WinMatchId = progression.WinProgressionId,
            Progression = progression,
            LoseMatchId = progression.LoseProgressionId
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
        Completed = DateTime.UtcNow;
        //Completion = new Completion(winnerId, DateTime.UtcNow);
    }

    public static bool HasByeOpponent(LocalMatch.Match<Participant> match, Participant bye)
    {
        return match.Opponent1.Id == bye.Id || match.Opponent2.Id == bye.Id;
    }

    public static Participant GetNonByeOpponent(LocalMatch.Match<Participant> match, Participant bye)
    {
        return match.Opponent1.Id == bye.Id ? match.Opponent2 : match.Opponent1;
    }
}