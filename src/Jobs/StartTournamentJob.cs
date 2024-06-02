using Quartz;

namespace OpenTournament.Jobs;

[DisallowConcurrentExecution]
public sealed class StartTournamentJob(ILogger<StartTournamentJob> logger,
    AppDbContext appDbContext) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        /*
        var tournaments = await appDbContext
            .Tournaments
            .ToArrayAsync(context.CancellationToken);

        if (tournaments.Count == 0)
            return ;
        
        foreach(var tournament in tournaments) {
            tournament.Start();
        }
        */
        return;
    }
}