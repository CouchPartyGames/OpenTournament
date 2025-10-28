using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Api.Data.Models;

public sealed class EventAdmin
{
    public required EventId EventId { get; init; }
    
    public required ParticipantId ParticipantId { get; init; }
}