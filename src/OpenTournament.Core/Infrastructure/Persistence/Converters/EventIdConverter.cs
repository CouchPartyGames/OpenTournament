using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class EventIdConverter() : ValueConverter<EventId, Guid>(eventId => eventId.Value,
    guid => new EventId(guid));