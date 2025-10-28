using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public enum CompetitionType
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss
}

public enum GameMode
{
    Individual,
    Team
}


[Table("Competitions")]
public sealed class Competition
{
    public enum Visibility
    {
        Public,
        Private
    };
    
    [Column(TypeName = "varchar(36)")]
    public required CompetitionId CompetitionId { get; init; }
    
    [Column(TypeName = "varchar(50)")]
    public required string Name { get; init; }
    
    [Column(TypeName = "varchar(36)")]
    public GameId GameId { get; private set; }
    
    public string Rules { get; set; } = String.Empty;

    public GameMode Mode { get; set; } = GameMode.Individual;
    
    public Visibility CompetitionVisibility { get; private set; } = Visibility.Public;
}