using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;


public class Platform
{
    //public required PlatformId PlatformId { get; init; }
    public required int PlatformId { get; init; }
    
    [Column(TypeName = "varchar(25)")]
    public required string Name { get; init; }
    
    [Column(TypeName = "varchar(128)")]
    public string ImageUrl { get; init; } = string.Empty;
}