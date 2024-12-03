using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Data.ValueConverters;

namespace OpenTournament.Api.Data.EntityMapping;

public sealed class OutboxConfiguration : IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
            // Participants
        builder.HasKey(m => m.Id);

        builder
            .Property(o => o.Id)
            .HasConversion<OutboxIdConverter>();
    }
}