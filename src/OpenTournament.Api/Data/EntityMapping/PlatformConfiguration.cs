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
            new Platform { PlatformId = new PlatformId(new Guid("0193f4b3-4938-79bc-9bf4-a9f1b693730e")), Name = "XBox Series X" },
            new Platform { PlatformId = new PlatformId(new Guid("0193f4b6-873e-7d40-a6dd-bd898f206abb")), Name = "Playstation 5" },
            new Platform { PlatformId = new PlatformId(new Guid("0193f4b6-d6b8-7191-a2b8-07cc4c8a86fc")), Name = "Nintendo Switch" },
            new Platform { PlatformId = new PlatformId(new Guid("0193f4b7-078f-79cd-ba3b-f06d448481f5")), Name = "PC" }
        );
    }
}
