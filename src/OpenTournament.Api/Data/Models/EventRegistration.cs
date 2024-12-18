namespace OpenTournament.Api.Data.Models;

public sealed class EventRegistration
{
    public required EventId EventId { get; set; }
    
    public required ParticipantId UserId { get; set; }
    
    public DateTime JoinOnUtc { get; }
}