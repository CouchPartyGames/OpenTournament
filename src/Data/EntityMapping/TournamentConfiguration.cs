using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.EntityMapping;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
            // Tournaments
        builder.HasKey(t => t.Id);

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
            .HasConversion<int>();
        
        builder
            .Property(t => t.RegistrationMode)
            .HasConversion<int>();
        
    }
}