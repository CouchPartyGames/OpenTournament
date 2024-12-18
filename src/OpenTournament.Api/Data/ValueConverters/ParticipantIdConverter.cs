using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data.ValueConverters;

public sealed class ParticipantIdConverter() : ValueConverter<ParticipantId, string>(participantId => participantId.Value,
    guidFromDb => new ParticipantId(guidFromDb));