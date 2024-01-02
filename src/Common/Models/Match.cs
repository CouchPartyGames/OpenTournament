namespace OpenTournament.Common.Models;

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
    public MatchId Id { get; set; }

    public MatchState State { get; set; } = MatchState.Ready;
    
    
    //Progression WinProgression;
    //Progression LoseProgression;
}