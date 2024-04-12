using OpenTournament.Common.Draw.Layout;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public sealed class TournamentStartedEventHandler(AppDbContext dbContext) : INotificationHandler<TournamentStartedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;

    public async ValueTask Handle(TournamentStartedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("Hello");
        // Add First Round Matches
        var tournament = await _dbContext
            .Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == notification.TournamentId, cancellationToken);
        
        var participants = await _dbContext
            .Registrations
            .Where(x => x.TournamentId == notification.TournamentId)
            .Select(x => x.Participant)
            .ToListAsync(cancellationToken: cancellationToken);
        
        const ParticipantOrder.Order order = ParticipantOrder.Order.Random;
        var participantOrder = ParticipantOrder.Create(order, participants);
        
        var positions = new FirstRoundPositions(notification.DrawSize);
        var matches = new CreateProgressionMatches(new CreateMatchIds(positions).MatchByIds);
        var draw = new SingleEliminationFirstRound(matches.MatchWithProgressions, participantOrder);
        
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var drawMatch in draw.Matches)
            {
                var match = Match.Create(notification.TournamentId, drawMatch);
                _dbContext.Add(match);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}