namespace OpenTournament.Data.DomainEvents;

public sealed class JoinedTournamentEventHandler : INotificationHandler<JoinedTournamentEvent>
{
    public ValueTask Handle(JoinedTournamentEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}