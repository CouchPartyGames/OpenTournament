namespace OpenTournament.Api.Data.Models;

public sealed record GameId(Guid Value)
{
    public static GameId New() => new(Guid.CreateVersion7());
}