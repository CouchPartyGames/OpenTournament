using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace Jobs;

public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger) : IConsumer<TournamentStarted>
{
    public Task Consume(ConsumeContext<TournamentStarted> context)
    {
        logger.LogInformation("Tournament Started Consumer");

        var tournamentId = context.Message.TournamentId;

        return Task.CompletedTask;
    }
}