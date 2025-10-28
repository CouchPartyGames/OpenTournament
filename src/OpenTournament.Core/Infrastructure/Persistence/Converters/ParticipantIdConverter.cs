using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Infrastructure.Persistence.Converters;

public sealed class ParticipantIdConverter() : ValueConverter<ParticipantId, string>(participantId => participantId.Value,
    guidFromDb => new ParticipantId(guidFromDb));