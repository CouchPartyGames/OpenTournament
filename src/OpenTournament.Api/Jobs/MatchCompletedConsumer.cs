using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class MatchCompletedConsumer(AppDbContext dbContext, 
    ILogger<MatchCompletedConsumer> logger) : IConsumer<MatchCompletedEvent>
{
    public Task Consume(ConsumeContext<MatchCompletedEvent> context)
    {
        logger.LogInformation("Match Completed Consumer");

        var matchId = context.Message.MatchId;
        var match = dbContext.Matches.Select(m => m.Id == matchId);

        //match.LocalMatchId 

        throw new NotImplementedException();
        return Task.CompletedTask;
    }

    public bool IsTournamentComplete() {
        return false;
    }
}