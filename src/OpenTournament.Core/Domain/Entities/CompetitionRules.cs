namespace OpenTournament.Api.Data.Models;

public sealed class CompetitionRules
{
    public CompetitionId CompetitionId { get; set; }
    
    public string Rules { get; set; } = string.Empty;
}