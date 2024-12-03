using System.Text.Json.Serialization;

namespace OpenTournament.Api.DomainEvents;

[JsonDerivedType(typeof(TournamentStartedEvent), typeDiscriminator: "TournamentStartedEvent")]
[JsonDerivedType(typeof(MatchCompletedEvent), typeDiscriminator: "MatchCompletedEvent")]
public interface IDomainEvent : INotification;