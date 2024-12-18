using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

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


public sealed class Competition
{
    public enum Visibility
    {
        Public,
        Private
    };
    
    [Column(TypeName = "varchar(36)")]
    public CompetitionId CompetitionId { get; private set; }
    
    [Column(TypeName = "varchar(50)")]
    public string Name { get; private set; }
    
    [Column(TypeName = "varchar(36)")]
    public GameId GameId { get; private set; }
    
    public string Rules { get; set; } = String.Empty;

    public GameMode Mode { get; set; } = GameMode.Individual;
    
    public Visibility CompetitionVisibility { get; private set; } = Visibility.Public;
}