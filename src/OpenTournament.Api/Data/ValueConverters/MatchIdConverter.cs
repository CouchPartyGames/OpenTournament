using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.ValueConverters;

public sealed class MatchIdConverter : ValueConverter<MatchId, Guid>
{
    public MatchIdConverter() : base(v => v.Value,
        v => new MatchId(v))
    {
    }    
}