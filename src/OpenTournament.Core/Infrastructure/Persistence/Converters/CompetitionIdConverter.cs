using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class CompetitionIdConverter() : ValueConverter<CompetitionId, Guid>(competitionId => competitionId.Value,
    guid => new CompetitionId(guid));