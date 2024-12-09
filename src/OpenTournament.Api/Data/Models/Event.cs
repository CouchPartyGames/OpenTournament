using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public sealed record EventId(Guid Value);

public interface ILocation;

public sealed record Location(string Address1, string Address2, string City, string State, string ZipCode) : ILocation;

public sealed record OnlineLocation();


public sealed class Event
{
    public enum State
    {
        Draft,
        Published
    }
    public enum Visibility
    {
        Public,
        Private
    }
    
    [Column(TypeName = "varchar(36)")]
    public EventId EventId { get; init; }
    
    [Column(TypeName = "varchar(50)")]
    public string Name { get; init; }
    
    public Location Location { get; init; }
    
    public Visibility EventVisibility { get; init; } = Visibility.Public;

    public State EventState { get; init; } = State.Draft;
}