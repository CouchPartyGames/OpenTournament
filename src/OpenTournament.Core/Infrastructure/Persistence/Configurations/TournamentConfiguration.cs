using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Converters;

namespace OpenTournament.Core.Infrastructure.Persistence.Configurations;

public sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
            // Tournaments
        builder.HasKey(t => t.Id);

        builder
            .HasMany(t => t.Matches)
            .WithOne()
            .HasForeignKey(m => m.TournamentId)
            .IsRequired();

        builder
            .Property(t => t.Id)
            .HasConversion<TournamentIdConverter>();
        
        builder
            .Property(b => b.Name)
            .IsRequired();

        builder
            .Property(t => t.EliminationMode)
            .HasConversion<int>();
            //.HasDefaultValue(EliminationMode.);
        
        builder
            .Property(t => t.DrawSize)
            .HasConversion<TournamentDrawSizeConverter>();
        
        builder
            .Property(t => t.RegistrationMode)
            .HasConversion<int>();


        builder.ComplexProperty(x => x.Creator, property =>
        {
            property.Property(p => p.CreatorId)
                .HasColumnName("CreatorId")
                .HasConversion<ParticipantIdConverter>();

            property.Property(p => p.CreatedOnUtc)
                .HasColumnName("CreatedOnUtc");
        });

        /*builder
            .Property(t => t.CreatorId)
            .HasConversion<ParticipantIdConverter>();*/

    }
}