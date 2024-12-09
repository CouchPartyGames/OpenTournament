namespace OpenTournament.Api.Data.Models;

public sealed record PlatformId(int Value);

public class Platform
{
    public required PlatformId PlatformId { get; init; }
    
    public required string Name { get; init; }
}