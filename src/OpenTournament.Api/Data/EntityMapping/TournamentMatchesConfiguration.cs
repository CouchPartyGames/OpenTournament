using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Data.ValueConverters;

namespace OpenTournament.Api.Data.EntityMapping;

public class TournamentMatchesConfiguration : IEntityTypeConfiguration<TournamentMatches>
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
            .IsRequired();
    }
}