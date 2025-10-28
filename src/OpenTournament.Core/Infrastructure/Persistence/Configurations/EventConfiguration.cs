using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

// Setup Data Modeling
public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // primary key
        builder.HasKey(e => e.EventId);

        builder
            .Property(e => e.EventId)
            .HasConversion<EventIdConverter>();
    }
}