using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Data.ValueConverters;

namespace OpenTournament.Api.Data.EntityMapping;

public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
            // Participants
        builder.HasKey(m => m.Id);
        
        builder
            .Property(p => p.Id)
            .HasConversion<ParticipantIdConverter>();

        builder.HasData(Participant.CreateBye());
    }
}