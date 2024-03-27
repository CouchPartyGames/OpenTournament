using OpenTournament.Data.Models;

namespace OpenTournament.Data.Events;

public record TournamentCreated(TournamentId TournamentId);

public record TournamentUpdated(TournamentId TournamentId);

public record TournamentDrawFinalized(TournamentId TournamentId);

public record TournamentReady(TournamentId TournamentId);

public record TournamentStarted(TournamentId TournamentId);

public record TournamentCompleted(TournamentId TournamentId);

public record TournamentCancelled(TournamentId TournamentId, string Reason);