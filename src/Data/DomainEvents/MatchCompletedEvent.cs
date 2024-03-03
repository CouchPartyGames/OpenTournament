using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed record MatchCompletedEvent(MatchId MatchId) : INotification;