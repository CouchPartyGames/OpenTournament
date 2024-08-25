using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.Models;
using OpenTournament.Data.ValueConverters;

namespace OpenTournament.Data.EntityMapping;

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