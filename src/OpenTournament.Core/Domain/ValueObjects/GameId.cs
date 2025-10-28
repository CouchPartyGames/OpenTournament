namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record GameId(Guid Value)
{
    public static GameId New() => new(Guid.CreateVersion7());
}