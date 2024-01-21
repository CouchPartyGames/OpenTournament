using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Common.Models;

namespace OpenTournament.Common.Data.EntityMapping;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
            // Participants
        builder.HasKey(m => m.Id);
        
        builder
            .Property(p => p.Id)
            .HasConversion(v => v.Value,
                v => new ParticipantId(v));

        builder.HasData(new Participant
        {
            Id = ParticipantId.TryParse("f924898c-249c-4ff3-b483-5dfe2819a66d"), 
            Name = "Bye"
        });
    }
}