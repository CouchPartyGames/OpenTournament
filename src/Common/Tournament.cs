namespace OpenTournament.Common;

public sealed record TournamentId(Guid Value);


public sealed class Tournament
{
    //public TournamentId Id { get; set; }
    public Guid Id { get; set; }
    public required string Name { get; set; }   
    
    //public DateTime StartTime { get; set;  }


    public enum Status
    {
        InProcess,
        Completed
    }
}