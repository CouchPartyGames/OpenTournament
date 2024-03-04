using System.ComponentModel.DataAnnotations.Schema;
using OneOf.Types;

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
    
    public static TournamentId NewTournamentId() => new(Guid.NewGuid());
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

public enum DrawSize
{
    NotSet = 0,
    Size2 = 2,
    Size4 = 4,
    Size8 = 8,
    Size16 = 16,
    Size32 = 32,
    Size64 = 64,
    Size128 = 128
};

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

    public DrawSize DrawSize = DrawSize.NotSet;

    public DrawSeeding DrawSeeding = DrawSeeding.Random;

    public RegistrationMode RegistrationMode = RegistrationMode.Dynamic;

    //public Participant Creator { get; set; }
    //public ParticipantId CreatorId { get; set; }

    public ICollection<Match> Matches { get; init; }

    public void Start(DrawSize size)
    {
        DrawSize = size;
        Status = Status.InProcess;
    }
}