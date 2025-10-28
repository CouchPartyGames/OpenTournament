using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public sealed class Results
{
    Dictionary<ParticipantId, int> Ordering { get; set; }
}