using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public sealed class CompetitionAdmin
{
    public required CompetitionId CompetitionId { get; init; }
    
    public required ParticipantId ParticipantId { get; init; }
}