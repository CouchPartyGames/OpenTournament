using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class PoolIdConverter() : ValueConverter<PoolId, Guid>(poolId => poolId.Value,
    guidFromDb => new PoolId(guidFromDb));
