namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentStartedEventHandler(AppDbContext dbContext) : INotificationHandler<TournamentStartedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;

    public async ValueTask Handle(TournamentStartedEvent notification, CancellationToken cancellationToken)
    {
        // Add First Round Matches
        var tournament = await _dbContext
            .Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == notification.TournamentId);
        
        /*
        var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            
            tournament.DrawSize;
            foreach (var match in Matches)
            {
                _dbContext.AddAsync(match);
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
        }

        // Notify Others
        await Task.CompletedTask();
        */
        
        throw new NotImplementedException();
    }
}