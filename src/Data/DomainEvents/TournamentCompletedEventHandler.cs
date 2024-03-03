namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentCompletedEventHandler : INotificationHandler<TournamentCompletedEvent>
{
    public ValueTask Handle(TournamentCompletedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}