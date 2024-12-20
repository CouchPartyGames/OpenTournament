using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public interface ILocation;

public sealed record Location(string Address1, string Address2, string City, string State, string ZipCode) : ILocation;



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
    public required EventId EventId { get; init; }
    
    [Column(TypeName = "varchar(50)")]
    public string Name { get; init; }
    
    public string Description { get; init; }
    
    public string Slug { get; init; }
    
    //public Location Location { get; init; }
    
    public Visibility EventVisibility { get; init; } = Visibility.Public;

    public State EventState { get; init; } = State.Draft;
    
    public List<Competition> Competitions { get; init; } = new List<Competition>();
}