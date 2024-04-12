using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents.Handlers;

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
            .FirstOrDefaultAsync(m => m.Id == notification.MatchId, cancellationToken);

            // Create New Match
        bool addMatch = false;
        if (addMatch)
        {
            /*
            match.LocalMatchId;
            match.WinMatchId;
            */
            
            //await _dbContext.AddAsync(Match.Create(notification.TournamentId, ));
        }
        

        bool markTournamentAsCompleted = false;
        if (markTournamentAsCompleted)
        {
            await _dbContext.Tournaments
                .Where(t => t.Id == notification.TournamentId)
                .ExecuteUpdateAsync(setters => 
                    setters
                        .SetProperty(t => t.Status, Status.Completed)
                        .SetProperty(t => t.Completed, new DateTime()), 
                    cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        throw new NotImplementedException();
    }
}