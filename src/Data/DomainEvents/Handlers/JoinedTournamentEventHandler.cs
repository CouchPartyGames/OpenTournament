namespace OpenTournament.Data.DomainEvents.Handlers;

public sealed class RegistrationJoinedEventHandler : INotificationHandler<JoinedTournamentEvent>
{
    public ValueTask Handle(JoinedTournamentEvent notification, CancellationToken cancellationToken)
    {
        // Notify Others
        throw new NotImplementedException();
    }
}