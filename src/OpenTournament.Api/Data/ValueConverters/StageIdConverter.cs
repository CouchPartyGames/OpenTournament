using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;


public sealed class StageIdConverter() : ValueConverter<StageId, Guid>(stageId => stageId.Value,
    guidFromDb => new StageId(guidFromDb));
