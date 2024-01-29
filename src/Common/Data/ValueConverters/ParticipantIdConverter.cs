using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Common.Models;

namespace OpenTournament.Common.Data.ValueConverters;

public sealed class ParticipantIdConverter : ValueConverter<ParticipantId, Guid>
{
    public ParticipantIdConverter() : base(v => v.Value,
        v => new ParticipantId(v))
    {
        
    } 
}