using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class GameIdConverter() : ValueConverter<GameId, Guid>(gameId => gameId.Value,
    intFromDb => new GameId(intFromDb));