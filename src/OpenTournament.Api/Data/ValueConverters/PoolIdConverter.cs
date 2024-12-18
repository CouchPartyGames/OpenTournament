using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class PoolIdConverter() : ValueConverter<PoolId, Guid>(poolId => poolId.Value,
    guidFromDb => new PoolId(guidFromDb));
