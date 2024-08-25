using System.Text.Json.Serialization;
using OpenTournament.Data.DomainEvents;

namespace OpenTournament.Data;

[JsonDerivedType(typeof(TournamentStartedEvent), typeDiscriminator: "TournamentStartedEvent")]
[JsonDerivedType(typeof(MatchCompletedEvent), typeDiscriminator: "MatchCompletedEvent")]
public interface IDomainEvent : INotification;