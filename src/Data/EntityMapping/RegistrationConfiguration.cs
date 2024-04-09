using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.EntityMapping;

public sealed class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder
            .HasKey(r => new { r.TournamentId, r.ParticipantId });
        
        builder
            .Property(p => p.TournamentId)
            .HasConversion<TournamentIdConverter>();

        builder
            .Property(p => p.ParticipantId)
            .HasConversion<ParticipantIdConverter>();
            //.HasColumnType("varchar(30)");
            
    }
}