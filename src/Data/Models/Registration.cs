namespace OpenTournament.Data.Models;

public sealed class Registration
{
    public TournamentId TournamentId { get; init; }

    public ParticipantId ParticipantId { get; init; }
    
    public Participant Participant { get; init; }
}