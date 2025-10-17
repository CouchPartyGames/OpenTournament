namespace OpenTournament.Api.Data.Models;

public sealed record StageId(Guid Value)
{
    public static StageId New() => new(Guid.CreateVersion7());
}