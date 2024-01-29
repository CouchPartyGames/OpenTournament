using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Common.Data.ValueConverters;
using OpenTournament.Common.Models;

namespace OpenTournament.Common.Data.EntityMapping;

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
            .HasConversion<ParticipantId>();
    }
}