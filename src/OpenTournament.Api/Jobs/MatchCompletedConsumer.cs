using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class MatchCompletedConsumer : IConsumer<MatchCompletedEvent>
{
    public Task Consume(ConsumeContext<MatchCompletedEvent> context)
    {
        throw new NotImplementedException();
    }
}