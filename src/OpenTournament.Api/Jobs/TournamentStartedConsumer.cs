using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger) : IConsumer<TournamentStartedEvent>
{
    public Task Consume(ConsumeContext<TournamentStartedEvent> context)
    {
        logger.LogInformation("Tournament Started Consumer");

        var tournamentId = context.Message.TournamentId;

        throw new NotImplementedException();
        return Task.CompletedTask;
    }
}