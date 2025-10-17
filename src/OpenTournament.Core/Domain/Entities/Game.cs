using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

[Table("Games")]
public sealed class Game
{
    [Column(TypeName = "varchar(36)")]
    public required GameId GameId { get; init; }
    
    [Column(TypeName = "varchar(36)")]
    public required string Name { get; init; }
    
    public string Image { get; init; } = string.Empty;
    
    // Platforms
}