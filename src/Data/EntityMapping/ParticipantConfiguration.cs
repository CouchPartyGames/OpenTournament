using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Models;

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
            Id = ParticipantId.TryParse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), 
            Name = "Bye"
        });
    }
}