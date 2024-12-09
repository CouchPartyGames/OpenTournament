using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public sealed record CompetitionId(Guid Value);

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
    
    public GameId GameId { get; private set; }

    public Visibility CompetitionVisibility { get; private set; } = Visibility.Public;
}