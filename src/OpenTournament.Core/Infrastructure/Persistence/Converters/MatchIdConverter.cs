using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class MatchIdConverter() : ValueConverter<MatchId, Guid>(matchId => matchId.Value,
    guidFromDb => new MatchId(guidFromDb));