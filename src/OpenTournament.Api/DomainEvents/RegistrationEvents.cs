using OpenTournament.Data.Models;
using MassTransit;

namespace OpenTournament.Data.DomainEvents;


// <summary>
// Allow Participants to register for the tournament
// </summary>
public record RegistrationOpened(TournamentId TournamentId);
// Registration is complete
public record RegistrationFinalized(TournamentId TournamentId);


public record PlayerJoined {
    public TournamentId TournamentId { get; init; }
    public ParticipantId ParticipantId { get; init; }
}

public record PlayerLeft {
    public TournamentId TournamentId { get; init;}
    public ParticipantId ParticipantId { get; init; }
}


//public record JoinedTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;
//public record LeftTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;