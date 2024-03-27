using OpenTournament.Data.Models;

namespace OpenTournament.Data.Events;

public record MatchCreated(MatchId MatchId);

public record MatchReady(MatchId MatchId);

public record MatchInProgress(MatchId MatchId);

public record MatchCompleted(MatchId MatchId);

public record MatchForfeited(MatchId MatchId, string Reason);