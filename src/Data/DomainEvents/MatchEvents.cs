using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public record MatchCompletedEvent(MatchId MatchId) : INotification, IDomainEvent;
public record MatchCompleted(MatchId MatchId);

public record MatchCreated(MatchId MatchId);

public record MatchReady(MatchId MatchId);

public record MatchInProgress(MatchId MatchId);

public record MatchForfeited(MatchId MatchId, string Reason);
