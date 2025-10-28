namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record CompetitionId(Guid Value)
{
    public static CompetitionId New() => new(Guid.CreateVersion7());
}