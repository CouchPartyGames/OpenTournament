using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

public class TournamentMatches
{
    public TournamentId TournamentId { get; set; }
    
    [Column(TypeName = "jsonb")]
    public List<MatchMetadata> Matches { get; set; }
}