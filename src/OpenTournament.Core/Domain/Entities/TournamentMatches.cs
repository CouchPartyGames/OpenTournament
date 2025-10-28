using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public class TournamentMatches
{
    public TournamentId TournamentId { get; set; }
    
    [Column(TypeName = "jsonb")]
    public List<MatchMetadata> Matches { get; set; }
}