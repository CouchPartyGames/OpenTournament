using System.Text.Json;
using OpenTournament.Common.Draw.Layout;
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
        if (ShouldAddMatch())
        {
            /*
            match.LocalMatchId;
            match.WinMatchId;
            */

            //var match = new SingleEliminationFirstRound.SingleMatch();
            //await _dbContext.AddAsync(Match.Create(notification.TournamentId, match));
        }
        
            // Add Opponent to Existing Match
        if (ShouldAddOpponentToMatch())
        {
            //await _dbContext.Matches.ExecuteUpdateAsync(cancellationToken: cancellationToken);
        }

            // Complete Tournament
        if (IsTournamentCompleted())
        {
            await _dbContext.Tournaments
                .Where(t => t.Id == notification.TournamentId)
                .ExecuteUpdateAsync(setters => 
                    setters
                        .SetProperty(t => t.Status, Status.Completed)
                        .SetProperty(t => t.Completed, new DateTime()), 
                    cancellationToken);

            await _dbContext.Outboxes.AddAsync(Outbox.Create(new TournamentCompletedEvent(notification.TournamentId)), cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        throw new NotImplementedException();
    }

    bool IsTournamentCompleted()
    {
        return false;
    }

    bool ShouldAddOpponentToMatch()
    {
        return false;
    }

    bool ShouldAddMatch()
    {
        return false;
    }
}