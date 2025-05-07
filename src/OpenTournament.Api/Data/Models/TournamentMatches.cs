namespace OpenTournament.Api.Data.Models;

public class TournamentMatches
{
    public TournamentId TournamentId { get; set; }
    
    public List<MatchMetadata> Matches { get; set; }
}