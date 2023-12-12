using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Common;


[ComplexType]
public sealed record TournamentId(Guid Value);

public enum Status
{
    Registration,
    InProcess,
    Completed
}

[ComplexType]
public sealed class Tournament
{
    [Required]
    public TournamentId Id { get; set; }
    //public Guid Id { get; set; }
    public required string Name { get; set; }   
    
    public Status Status { get; set; }
    //public DateTime StartTime { get; set;  }
}