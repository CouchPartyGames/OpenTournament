using System.Text.Json;
using System.Text.Json.Nodes;
using OpenTournament.Data.Models;
using Quartz;

namespace OpenTournament.Services;

[DisallowConcurrentExecution]
public sealed class OutboxBackgroundJob(ILogger<OutboxBackgroundJob> logger, 
    AppDbContext appDbContext,
    IMediator mediator) : IJob
{
    public readonly int NumOfMessages = 20;
    
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Running Background Job");
        try
        {
            
            var messages = await appDbContext.Set<Outbox>()
                .Where(o => o.State == Outbox.Status.Ready)
                .Take(NumOfMessages)
                .ToListAsync(context.CancellationToken);
            
            if (messages.Count == 0)
                return;
            
            await using var transaction = await appDbContext.Database.BeginTransactionAsync(context.CancellationToken);
            foreach (var message in messages)
            {
                var eventObj = JsonSerializer.Deserialize<IDomainEvent>(message.Content);
                if (eventObj is null)
                {
                    continue;
                }
                
                await mediator.Publish(eventObj, context.CancellationToken);
                message.SetProcessed();
            }
            
            await appDbContext.SaveChangesAsync(context.CancellationToken);
            await appDbContext.Database.CommitTransactionAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            await appDbContext.Database.RollbackTransactionAsync(context.CancellationToken);
        }
    }
}