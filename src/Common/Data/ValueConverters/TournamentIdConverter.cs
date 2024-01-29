using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Common.Models;

namespace OpenTournament.Common.Data.ValueConverters;


public sealed class TournamentIdConverter : ValueConverter<TournamentId, Guid>
{
    public TournamentIdConverter() : base(
        v => v.Value,
        v => new TournamentId(v)
    )
    {
    }
}