using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class ParticipantIdConverter : ValueConverter<ParticipantId, string>
{
    public ParticipantIdConverter() : base(v => v.Value,
        v => new ParticipantId(v))
    {
        
    } 
}