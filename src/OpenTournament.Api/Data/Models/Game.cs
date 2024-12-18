using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public sealed class Game
{
    [Column(TypeName = "varchar(36)")]
    public required GameId GameId { get; init; }
    
    [Column(TypeName = "varchar(36)")]
    public string Name { get; init; }
    
    public string Url { get; init; }
    
    // Platforms
}