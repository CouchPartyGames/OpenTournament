using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
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

        /*builder
            .ComplexProperty(m => m.Completion, property =>
                {
                    property.Property(p => p.WinnerId)
                        .HasColumnName("WinnerId2")
                        .HasConversion<ParticipantIdConverter>();

                    property.Property(p => p.CompletedOnUtc)
                        .HasColumnName("CompletedOnUtc")
                        .IsRequired(true)
                        .HasDefaultValueSql("null");

                });*/

        builder
            .ComplexProperty(m => m.Progression, property => 
            {
                property.Property(p => p.WinProgressionId)
                    .HasColumnName("WinProgressionId")
                    .HasDefaultValue(Progression.NoProgression);
                
                property.Property(p => p.LoseProgressionId)
                    .HasColumnName("LoseProgressionId")
                    .HasDefaultValue(Progression.NoProgression);
            });
    }
}