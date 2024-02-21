using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.EntityMapping;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
            // Participants
        builder.HasKey(m => m.Id);
        
        builder
            .Property(p => p.Id)
            .HasConversion<ParticipantIdConverter>();

        builder.HasData(new Participant
        {
            Id = new ParticipantId("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), 
            Name = "Bye"
        });
    }
}