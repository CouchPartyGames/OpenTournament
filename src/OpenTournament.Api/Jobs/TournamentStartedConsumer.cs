using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class TournamentStartedConsumer : IConsumer<TournamentStartedEvent>
{
    public Task Consume(ConsumeContext<TournamentStartedEvent> context)
    {
        var tournamentId = context.Message.TournamentId;

        throw new NotImplementedException();
        return Task.CompletedTask;
    }
}