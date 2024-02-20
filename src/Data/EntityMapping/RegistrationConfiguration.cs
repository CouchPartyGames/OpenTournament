using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Models;

namespace OpenTournament.Data.EntityMapping;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
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
    }
}