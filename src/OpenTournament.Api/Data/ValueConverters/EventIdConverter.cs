using Models = OpenTournament.Api.Data.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class EventIdConverter : ValueConverter<Models.EventId, Guid>
{
    public EventIdConverter() : base(eventId => eventId.Value,
        guid => new Models.EventId(guid))
    {
    }
}