namespace OpenTournament.Data.DomainEvents;

public sealed class LeftTournamentEventHandler : INotificationHandler<LeftTournamentEvent>
{
    public ValueTask Handle(LeftTournamentEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}