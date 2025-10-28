namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record StageId(Guid Value)
{
    public static StageId New() => new(Guid.CreateVersion7());
}