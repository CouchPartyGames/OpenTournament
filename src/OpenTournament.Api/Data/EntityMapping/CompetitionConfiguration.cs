using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Data.ValueConverters;

namespace OpenTournament.Api.Data.EntityMapping;

public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder
            .HasKey(c => c.CompetitionId);

        builder
            .Property(c => c.CompetitionId)
            .HasConversion<CompetitionIdConverter>();
        
        builder.Property(c => c.GameId)
            .HasConversion<GameIdConverter>();
    }
}