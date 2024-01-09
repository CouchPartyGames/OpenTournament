
using System.ComponentModel.DataAnnotations.Schema;
using OneOf.Types;

namespace OpenTournament.Common.Models;

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
    
    public static TournamentId Create() => new(Guid.NewGuid());
}

public enum Status
{
    Registration,
    InProcess,
    Completed
}

public enum EliminationMode
{
    Single = 0,
    Double
};

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
    public TournamentId Id { get; set; }
    
    public required string Name { get; set; }

    public Status Status { get; set; } = Status.Registration;
    
    public DateTime StartTime { get; set;  }

    public int MinParticipants { get; set; } = 2;

    public int MaxParticipants { get; set; } = 8;

    public EliminationMode EliminationMode = EliminationMode.Single;

    public DrawSize DrawSize = DrawSize.NotSet;

    public RegistrationMode RegistrationMode = RegistrationMode.Dynamic;
}