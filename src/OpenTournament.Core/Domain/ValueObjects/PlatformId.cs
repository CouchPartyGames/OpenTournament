namespace OpenTournament.Api.Data.Models;

public sealed record PlatformId(Guid Value)
{
    public static PlatformId New() => new(Guid.CreateVersion7());
}
