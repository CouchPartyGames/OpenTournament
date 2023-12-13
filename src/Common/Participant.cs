namespace OpenTournament.Common;

public sealed record ParticipantId(Guid Value);

public sealed class Participant
{
    public ParticipantId Id { get; set; }
}