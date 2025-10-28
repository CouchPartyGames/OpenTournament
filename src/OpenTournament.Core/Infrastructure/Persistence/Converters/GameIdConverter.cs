using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class GameIdConverter() : ValueConverter<GameId, Guid>(gameId => gameId.Value,
    intFromDb => new GameId(intFromDb));