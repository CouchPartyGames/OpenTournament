using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class OutboxIdConverter : ValueConverter<OutboxId, Guid>
{
    public OutboxIdConverter() : base(o => o.Value,
        o => new OutboxId(o)) {}
    
}