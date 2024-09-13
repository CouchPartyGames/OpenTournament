namespace OpenTournament.Data.DomainEvents.Handlers;

public sealed class RegistationLeftEventHandler : INotificationHandler<LeftTournamentEvent>
{
    public ValueTask Handle(LeftTournamentEvent notification, CancellationToken cancellationToken)
    {
        // Notify Others
        throw new NotImplementedException();
    }
}