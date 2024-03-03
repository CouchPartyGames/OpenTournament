namespace OpenTournament.Data.DomainEvents;

public sealed class MatchCompletedEventHandler : INotificationHandler<MatchCompletedEvent>
{
    public ValueTask Handle(MatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}