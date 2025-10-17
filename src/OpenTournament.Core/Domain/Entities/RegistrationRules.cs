namespace OpenTournament.Api.Data.Models;

public sealed class RegistrationRules
{
    public enum Visibility
    {
        Public,
        Private,
        Hidden
    }
    
    public required CompetitionId CompetitionId { get; set; }
    
    public required DateTime StartTime { get; set; }
    
    public required DateTime EndTime { get; set; }
    
    public required Visibility RulesVisibility { get; set; } = Visibility.Public;
}