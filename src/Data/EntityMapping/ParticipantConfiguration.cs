﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenTournament.Data.ValueConverters;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.EntityMapping;

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