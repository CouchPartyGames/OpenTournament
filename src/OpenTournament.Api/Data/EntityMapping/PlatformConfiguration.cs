using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Data.ValueConverters;

namespace OpenTournament.Api.Data.EntityMapping;

public sealed class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> builder)
    {
        builder
            .HasKey(c => c.PlatformId);

        builder
            .Property(c => c.PlatformId)
            .HasConversion<PlatformIdConverter>();

        builder.HasData(
            new Platform { PlatformId = 1, Name = "XBox" },
            new Platform { PlatformId = 2, Name = "Playstation 5" },
            new Platform { PlatformId = 3, Name = "Nintendo Switch" },
            new Platform { PlatformId = 4, Name = "PC" }
        );
    }
}
