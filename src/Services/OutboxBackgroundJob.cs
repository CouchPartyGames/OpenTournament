using Quartz;

namespace OpenTournament.Services;

public sealed class OutboxBackgroundJob(ILogger<OutboxBackgroundJob> logger, 
    AppDbContext appDbContext) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        //appDbContext.
        logger.LogInformation("Running Background Job");
        return Task.CompletedTask;
    }
}