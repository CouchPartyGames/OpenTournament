using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.EntityMapping;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
            // Matches
        builder.HasKey(m => m.Id);
        
        builder
            .Property(m => m.Id)
            .HasConversion<MatchIdConverter>();

        builder
            .Property(m => m.Opponent1)
            .HasConversion<ParticipantIdConverter>();
        
        builder
            .Property(m => m.Opponent2)
            .HasConversion<ParticipantIdConverter>();
        
    }
}