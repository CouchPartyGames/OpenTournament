using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;


public sealed class StageIdConverter() : ValueConverter<StageId, Guid>(stageId => stageId.Value,
    guidFromDb => new StageId(guidFromDb));
