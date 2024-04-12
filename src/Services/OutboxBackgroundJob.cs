using System.Text.Json;
using OpenTournament.Data.Models;
using Quartz;

namespace OpenTournament.Services;

[DisallowConcurrentExecution]
public sealed class OutboxBackgroundJob(ILogger<OutboxBackgroundJob> logger, 
    AppDbContext appDbContext,
    IMediator mediator) : IJob
{
    private const int NumOfMessages = 20;

    public async Task Execute(IJobExecutionContext context)
    {
        var messages = await appDbContext.Set<Outbox>()
            .Where(o => o.State == Outbox.Status.Ready)
            .Take(NumOfMessages)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
            return;

        var executionStrategy = appDbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {

            try {
                await using var transaction =
                    await appDbContext.Database.BeginTransactionAsync(context.CancellationToken);
                foreach (var message in messages)
                {
                    IDomainEvent? eventObj = JsonSerializer.Deserialize<IDomainEvent>(message.Content);
                    if (eventObj is null)
                    {
                        logger.LogError($"Background job unable to determine domain event ({message.Id})");
                        continue;
                    }


                    Console.WriteLine(eventObj.ToString());
                    await mediator.Publish(eventObj, context.CancellationToken);
                    message.SetProcessed();
                }

                await appDbContext.SaveChangesAsync(context.CancellationToken);
                await appDbContext.Database.CommitTransactionAsync(context.CancellationToken);
            }
            catch (NotSupportedException e)
            {
                logger.LogError("Background Job transaction failed", e.ToString());
                await appDbContext.Database.RollbackTransactionAsync(context.CancellationToken);
            }
        });

    }
}