using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Models;

namespace OpenTournament.Data.EntityMapping;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
            // Matches
        builder.HasKey(m => m.Id);
        
        builder
            .Property(m => m.Id)
            .HasConversion(v => v.Value,
                v => new MatchId(v));
    }
}