using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Api.Data.Models;

public sealed class CompetitionRules
{
    public required CompetitionId CompetitionId { get; set; }
    
    public string Rules { get; set; } = string.Empty;
}