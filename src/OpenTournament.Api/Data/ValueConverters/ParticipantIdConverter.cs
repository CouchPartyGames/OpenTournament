using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.ValueConverters;

public sealed class ParticipantIdConverter : ValueConverter<ParticipantId, string>
{
    public ParticipantIdConverter() : base(v => v.Value,
        v => new ParticipantId(v))
    {
        
    } 
}