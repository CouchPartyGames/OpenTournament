using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

public sealed class TournamentMatchesConfiguration : IEntityTypeConfiguration<TournamentMatches>
{
    public void Configure(EntityTypeBuilder<TournamentMatches> builder)
    {
        builder.HasKey(t => t.TournamentId);
        
        builder
            .Property(t => t.TournamentId)
            .HasConversion<TournamentIdConverter>();
        
        builder
            .Property(t => t.Matches)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<MatchMetadata>>(v, (JsonSerializerOptions) null))
            .IsRequired();
        
    }
}