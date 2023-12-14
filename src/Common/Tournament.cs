
namespace OpenTournament.Common;

public sealed record TournamentId(Guid Value)
{
    public static TournamentId Create() => new TournamentId(Guid.NewGuid());
}

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

    public int MinParticipants { get; set; } = 2;

    public int MaxParticipants { get; set; } = 8;
}