namespace OpenTournament.Api.Data.Models;

public sealed record EventId(Guid Value)
{
    public static EventId New() => new EventId(Guid.CreateVersion7());
}