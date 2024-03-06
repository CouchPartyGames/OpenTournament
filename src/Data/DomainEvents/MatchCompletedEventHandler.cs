namespace OpenTournament.Data.DomainEvents;

public sealed class MatchCompletedEventHandler : INotificationHandler<MatchCompletedEvent>
{
    private readonly AppDbContext _dbContext;

    public MatchCompletedEventHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async ValueTask Handle(MatchCompletedEvent notification, CancellationToken cancellationToken)
    {
            // Get Completed Match
            // Get Next Matches
            // Or
            // No Matches, Tournament Complete

        var match = await _dbContext
            .Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == notification.MatchId);

        /*
        match.LocalMatchId;
        match.WinMatchId;
        */
        
        throw new NotImplementedException();
    }
}