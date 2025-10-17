namespace OpenTournament.Api.Data.Models;

public sealed record PoolId(Guid Value)
{
    public static PoolId New() => new(Guid.CreateVersion7());
}