using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;


public sealed class TournamentIdConverter : ValueConverter<TournamentId, Guid>
{
    public TournamentIdConverter() : base(
        v => v.Value,
        v => new TournamentId(v)
    )
    {
    }
}
