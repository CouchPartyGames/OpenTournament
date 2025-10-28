using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

public sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder
            .HasKey(c => c.StageId);

        builder
            .Property(c => c.StageId)
            .HasConversion<StageIdConverter>();
    }
}
