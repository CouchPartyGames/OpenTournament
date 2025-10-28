namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record PoolId(Guid Value)
{
    public static PoolId New() => new(Guid.CreateVersion7());
}