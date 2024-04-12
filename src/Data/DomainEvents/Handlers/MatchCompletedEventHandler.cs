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
            .FirstOrDefaultAsync(m => m.Id == notification.MatchId);

        /*
            // Create New Match
        bool addMatch = false;
        if (addMatch)
        {
            _dbContext.Add(new Match());
            await _dbContext.SaveChangesAsync();
        }
        */
        
        /*
        match.LocalMatchId;
        match.WinMatchId;
        */

        bool markTournamentAsCompleted = false;
        if (markTournamentAsCompleted)
        {
            var tournamentId = TournamentId.NewTournamentId();
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            tournament.Complete();
            await _dbContext.SaveChangesAsync();
        }

        throw new NotImplementedException();
    }
}