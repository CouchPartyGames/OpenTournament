namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentStartedEventHandler(AppDbContext dbContext) : INotificationHandler<TournamentStartedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;

    public ValueTask Handle(TournamentStartedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}