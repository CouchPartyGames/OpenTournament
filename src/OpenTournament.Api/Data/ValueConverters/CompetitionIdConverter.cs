using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class CompetitionIdConverter : ValueConverter<CompetitionId, Guid>
{
    public CompetitionIdConverter() : base(competitionId => competitionId.Value,
        guid => new CompetitionId(guid))
    {
    }
}