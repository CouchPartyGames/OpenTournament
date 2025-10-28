using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public sealed class EventRegistration
{
    public required EventId EventId { get; set; }
    
    public required ParticipantId UserId { get; set; }
    
    public DateTime JoinOnUtc { get; }

    public bool IsConfirmed { get; set; } = true;
}