using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.ValueConverters;

public sealed class OutboxIdConverter : ValueConverter<OutboxId, Guid>
{
    public OutboxIdConverter() : base(o => o.Value,
        o => new OutboxId(o)) {}
    
}