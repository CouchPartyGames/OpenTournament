namespace OpenTournament.Common;

public sealed record ParticipantId(Guid Value)
{
    public static ParticipantId Create() => new ParticipantId(Guid.NewGuid());
}

public sealed class Participant
{
    public ParticipantId Id { get; set; }
}