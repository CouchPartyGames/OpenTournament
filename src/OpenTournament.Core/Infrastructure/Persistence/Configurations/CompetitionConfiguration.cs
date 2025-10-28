using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

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