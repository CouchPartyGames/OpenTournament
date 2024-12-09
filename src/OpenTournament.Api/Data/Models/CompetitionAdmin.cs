namespace OpenTournament.Api.Data.Models;

public sealed class CompetitionAdmin
{
    public required CompetitionId CompetitionId { get; init; }
    
    public required ParticipantId ParticipantId { get; init; }
}