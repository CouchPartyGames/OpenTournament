namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentStartedEventHandler : INotificationHandler<TournamentStartedEvent>
{
    public ValueTask Handle(TournamentStartedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}