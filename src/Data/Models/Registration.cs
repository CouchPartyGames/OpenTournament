namespace OpenTournament.Data.Models;

public sealed class Registration
{
    public TournamentId TournamentId { get; set; }

    public ParticipantId ParticipantId { get; set; }
    
    public Participant Participant { get; set; }
}