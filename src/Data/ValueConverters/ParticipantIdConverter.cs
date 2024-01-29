using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Models;

namespace OpenTournament.Data.ValueConverters;

public sealed class ParticipantIdConverter : ValueConverter<ParticipantId, Guid>
{
    public ParticipantIdConverter() : base(v => v.Value,
        v => new ParticipantId(v))
    {
        
    } 
}