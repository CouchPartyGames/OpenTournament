using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;


[Table("Platforms")]
public class Platform
{
    //public required PlatformId PlatformId { get; init; }
    [Column(TypeName = "varchar(36)")]
    public required PlatformId PlatformId { get; init; }
    
    [Column(TypeName = "varchar(25)")]
    public required string Name { get; init; }
    
    [Column(TypeName = "varchar(128)")]
    public string ImageUrl { get; init; } = string.Empty;
}