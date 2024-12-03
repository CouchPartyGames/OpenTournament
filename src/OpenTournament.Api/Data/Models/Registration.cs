namespace OpenTournament.Api.Data.Models;

public sealed class Registration
{
    public TournamentId TournamentId { get; init; }

    public ParticipantId ParticipantId { get; init; }
    
    public Participant Participant { get; init; }

    public static Registration Create(TournamentId tournamentId, ParticipantId participantId) =>
        new Registration { TournamentId = tournamentId, ParticipantId = participantId };
}