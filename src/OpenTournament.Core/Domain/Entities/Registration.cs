using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

public sealed class Registration
{
    public TournamentId TournamentId { get; init; }

    public ParticipantId ParticipantId { get; init; }
    
    public Participant Participant { get; init; }

    public static Registration Create(TournamentId tournamentId, ParticipantId participantId) =>
        new Registration { TournamentId = tournamentId, ParticipantId = participantId };
}