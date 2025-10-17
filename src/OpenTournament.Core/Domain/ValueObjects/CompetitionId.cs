namespace OpenTournament.Api.Data.Models;

public sealed record CompetitionId(Guid Value)
{
    public static CompetitionId New() => new(Guid.CreateVersion7());
}