using CouchPartyGames.TournamentGenerator.Position;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OpenTournament.Data.ValueConverters;

public sealed class TournamentDrawSizeConverter : ValueConverter<DrawSize, int>
{
    public TournamentDrawSizeConverter() : base(
        size => (int)size.Value,
        db => DrawSize.NewRoundBase2(db))
    {
    }
}
