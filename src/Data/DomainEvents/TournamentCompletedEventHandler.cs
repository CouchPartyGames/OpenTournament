namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentCompletedEventHandler(AppDbContext dbContext) : INotificationHandler<TournamentCompletedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;
    
    public ValueTask Handle(TournamentCompletedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}