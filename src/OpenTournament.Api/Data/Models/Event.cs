namespace OpenTournament.Api.Data.Models;

public sealed record EventId(Guid Id);

public sealed record Location(string Address1, string Address2, string City, string State, string ZipCode);

public sealed class Event
{
    public EventId EventId { get; init; }
    
    public string Name { get; init; }
    
    public Location Location { get; init; }
}