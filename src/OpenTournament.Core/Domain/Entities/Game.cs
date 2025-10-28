using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

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