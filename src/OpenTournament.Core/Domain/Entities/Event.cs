using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public interface ILocation;

public sealed record Location(string Address1, string Address2, string City, string State, string ZipCode) : ILocation;



[Table("Events")]
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
    public required string Name { get; init; }
    
    public string Description { get; init; }
    
    public string Slug { get; init; }
    
    //public Location Location { get; init; }
    
    public Visibility EventVisibility { get; init; } = Visibility.Public;

    public State EventState { get; init; } = State.Draft;
    
    public List<Competition> Competitions { get; init; } = new List<Competition>();
}