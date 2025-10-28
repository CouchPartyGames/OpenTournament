namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record EventId(Guid Value)
{
    public static EventId New() => new EventId(Guid.CreateVersion7());
}