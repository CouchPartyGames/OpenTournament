using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class MatchIdConverter() : ValueConverter<MatchId, Guid>(matchId => matchId.Value,
    guidFromDb => new MatchId(guidFromDb));