using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class MatchCompletedConsumer : IConsumer<MatchCompletedEvent>
{
    public Task Consume(ConsumeContext<MatchCompletedEvent> context)
    {
        var matchId = context.Message.MatchId;
        throw new NotImplementedException();
        return Task.CompletedTask;
    }

    public bool IsTournamentComplete() {
        return false;
    }
}