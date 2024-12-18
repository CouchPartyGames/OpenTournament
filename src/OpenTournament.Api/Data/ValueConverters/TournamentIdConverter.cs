using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;


public sealed class TournamentIdConverter() : ValueConverter<TournamentId, Guid>(tournamentId => tournamentId.Value,
    guidFromDb => new TournamentId(guidFromDb));
