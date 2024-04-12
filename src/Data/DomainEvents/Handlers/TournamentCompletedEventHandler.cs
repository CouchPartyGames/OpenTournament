namespace OpenTournament.Data.DomainEvents.Handlers;

public sealed class TournamentCompletedEventHandler(AppDbContext dbContext) : INotificationHandler<TournamentCompletedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;
    
    public ValueTask Handle(TournamentCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Notify Others
        throw new NotImplementedException();
    }
}