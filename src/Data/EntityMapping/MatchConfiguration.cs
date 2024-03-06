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
            .Property(m => m.TournamentId)
            .HasConversion<TournamentIdConverter>();

        builder
            .Property(m => m.State)
            .HasConversion<int>();

        builder
            .Property(m => m.LocalMatchId)
            .IsRequired();

        builder
            .Property(m => m.Participant1Id)
            .HasConversion<ParticipantIdConverter>();
        
        builder
            .Property(m => m.Participant2Id)
            .HasConversion<ParticipantIdConverter>();

        builder
            .Property(m => m.WinnerId)
            .HasConversion<ParticipantIdConverter>();
        
        builder
            .Property(m => m.Created)
            .HasDefaultValueSql("now()");
        /*
        builder
            .HasOne(e => e.Participant1)
            .WithMany();
            */

        /*
        builder
            .HasOne(e => e.Participant2)
            .WithMany();
            */
    }
}