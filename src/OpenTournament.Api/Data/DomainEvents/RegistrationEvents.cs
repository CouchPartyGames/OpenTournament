using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;


// <summary>
// Allow Participants to register for the tournament
// </summary>
public record RegistrationOpened(TournamentId TournamentId);
// Registration is complete
public record RegistrationFinalized(TournamentId TournamentId);


public record JoinedTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;
public record PlayerJoined(TournamentId TournamentId, ParticipantId ParticipantId);

public record LeftTournamentEvent(TournamentId TournamentId, ParticipantId ParticipantId) : IDomainEvent;
public record PlayerLeft(TournamentId TournamentId, ParticipantId ParticipantId);