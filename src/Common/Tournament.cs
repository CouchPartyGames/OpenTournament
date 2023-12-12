
namespace OpenTournament.Common;

public sealed record TournamentId(Guid Value);

public enum Status
{
    Registration,
    InProcess,
    Completed
}

public sealed class Tournament
{
    public TournamentId Id { get; set; }
    
    public required string Name { get; set; }

    public Status Status { get; set; } = Status.Registration;
    
    public DateTime StartTime { get; set;  }
}