using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using CouchPartyGames.TournamentGenerator.Position;

namespace OpenTournament.Api.Data.Models;

public enum Status
{
    Registration = 0,
    InProcess,
    Completed
}

public enum EliminationMode
{
    [property: Description("Single Elimination Tournament")]
    Single = 0,
    [property: Description("Double Elimination Tournament")]
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


public sealed record Creator(ParticipantId CreatorId, DateTime CreatedOnUtc)
{
    public static Creator New(ParticipantId participantId) => new Creator(participantId, DateTime.UtcNow);
}

public sealed record ParticipantLimits(int MinOpponents, int MaxOpponents);

public sealed class Tournament
{
    [Column(TypeName = "varchar(36)")]
    public TournamentId Id { get; init; }
    
    [Column(TypeName = "varchar(50)")]
    public required string Name { get; set; }

    public Status Status { get; private set; } = Status.Registration;
    
    public DateTime StartedOnUtc { get; private set;  }

    public int MinParticipants { get; init; } = 2;

    public int MaxParticipants { get; init; } = 8;
    
    //public ParticipantLimits Limits { get; init; }

    public EliminationMode EliminationMode = EliminationMode.Single;

    public DrawSize? DrawSize { get; set; }

    public DrawSeeding DrawSeeding = DrawSeeding.Random;

    public RegistrationMode RegistrationMode = RegistrationMode.Dynamic;
    
    //public bool Has3rdPlace { get; init; }

    public required Creator Creator { get; init; }
    
    public DateTime? CompletedOnUtc { get; private set; }

    public ICollection<Match> Matches { get; init; }

    public void Start(DrawSize size)
    {
        DrawSize = size;
        Status = Status.InProcess;
        StartedOnUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = Status.Completed;
        CompletedOnUtc = DateTime.UtcNow;
    }
}