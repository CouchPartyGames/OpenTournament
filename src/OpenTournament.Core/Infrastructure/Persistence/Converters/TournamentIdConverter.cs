using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;


public sealed class TournamentIdConverter() : ValueConverter<TournamentId, Guid>(tournamentId => tournamentId.Value,
    guidFromDb => new TournamentId(guidFromDb));
