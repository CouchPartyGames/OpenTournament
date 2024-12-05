using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public sealed record GameId(int Value);

public class Game
{
    public GameId GameId { get; init; }
    
    [Column(TypeName = "varchar(36)")]
    public string Name { get; init; }
}