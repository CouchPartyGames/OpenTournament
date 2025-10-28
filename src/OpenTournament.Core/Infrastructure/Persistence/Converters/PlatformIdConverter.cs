using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class PlatformIdConverter() : ValueConverter<PlatformId, Guid>(platformId => platformId.Value,
    guidFromDb => new PlatformId(guidFromDb));
