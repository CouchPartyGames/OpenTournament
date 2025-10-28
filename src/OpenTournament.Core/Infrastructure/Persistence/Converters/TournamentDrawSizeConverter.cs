using CouchPartyGames.TournamentGenerator.Position;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class TournamentDrawSizeConverter() : ValueConverter<DrawSize, int>(size => (int)size.Value,
    db => DrawSize.NewRoundBase2(db));
