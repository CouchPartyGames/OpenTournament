using System.ComponentModel.DataAnnotations.Schema;
using CouchPartyGames.TournamentGenerator.Position;

namespace OpenTournament.Data.Models;

public sealed record TournamentId(Guid Value)
{
    public static TournamentId? TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new(guid);
    }
    
    public static TournamentId NewTournamentId() => new(Guid.CreateVersion7());
}

public enum Status
{
    Registration = 0,
    InProcess,
    Completed
}

public enum EliminationMode
{
    Single = 0,
    Double
};

public enum DrawSeeding {
    Random = 0,
    Seeded
}


public enum RegistrationMode
{
    Preset = 0,
    Dynamic
};

public sealed class Tournament
{
    [Column(TypeName = "varchar(36)")]
    public TournamentId Id { get; init; }
    
    public required string Name { get; set; }

    public Status Status { get; private set; } = Status.Registration;
    
    public DateTime StartTime { get; init;  }

    public int MinParticipants { get; init; } = 2;

    public int MaxParticipants { get; init; } = 8;

    public EliminationMode EliminationMode = EliminationMode.Single;

    public DrawSize? DrawSize { get; set; }

    public DrawSeeding DrawSeeding = DrawSeeding.Random;

    public RegistrationMode RegistrationMode = RegistrationMode.Dynamic;

    public DateTime Created { get; init; }
    //public Participant Creator { get; set; }
    //public ParticipantId CreatorId { get; set; }
    public DateTime Completed { get; init; }

    public ICollection<Match> Matches { get; init; }

    public void Start(DrawSize size)
    {
        DrawSize = size;
        Status = Status.InProcess;
    }

    public void Complete()
    {
        Status = Status.Completed;
    }
}