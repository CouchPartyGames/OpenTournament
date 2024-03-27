using OpenTournament.Data.Models;

namespace OpenTournament.Data.Events;


// Allow Participants to register for the tournament
public record RegistrationOpened(TournamentId TournamentId);

public record PlayerJoined(TournamentId TournamentId, ParticipantId ParticipantId);

public record PlayerLeft(TournamentId TournamentId, ParticipantId ParticipantId);

// Registration is complete
public record RegistrationFinalized(TournamentId TournamentId);