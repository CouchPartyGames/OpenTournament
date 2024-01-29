using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Models;

namespace OpenTournament.Data.ValueConverters;


public sealed class TournamentIdConverter : ValueConverter<TournamentId, Guid>
{
    public TournamentIdConverter() : base(
        v => v.Value,
        v => new TournamentId(v)
    )
    {
    }
}