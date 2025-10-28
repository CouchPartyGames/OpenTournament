using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Events;


// <summary>
// Allow Participants to register for the tournament
// </summary>
public record RegistrationOpened(TournamentId TournamentId);
// Registration is complete
public record RegistrationFinalized(TournamentId TournamentId);


public record PlayerJoined {
    public required TournamentId TournamentId { get; init; }
    public required ParticipantId ParticipantId { get; init; }
}

public record PlayerLeft {
    public required TournamentId TournamentId { get; init;}
    public required ParticipantId ParticipantId { get; init; }
}


//public record JoinedTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;
//public record LeftTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;