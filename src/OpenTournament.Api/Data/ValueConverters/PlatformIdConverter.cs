using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class PlatformIdConverter() : ValueConverter<PlatformId, Guid>(platformId => platformId.Value,
    guidFromDb => new PlatformId(guidFromDb));
