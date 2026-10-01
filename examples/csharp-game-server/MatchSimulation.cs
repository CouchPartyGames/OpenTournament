using System.Net;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.ReferenceGameServer;

/// <summary>The Match reports the simulation sends to Open Tournament.</summary>
public interface IMatchReports
{
    Task ReportStartedAsync(CancellationToken cancellationToken);
    Task ReportWinnerAsync(int bout, ParticipantId winner, CancellationToken cancellationToken);
}

/// <summary>The Match is not one the simulation can play.</summary>
public sealed class UnsupportedMatchException(string message) : Exception(message);

/// <summary>Plays a head-to-head Match by picking each Bout's winner at random.</summary>
public sealed class MatchSimulation(
    IMatchReports reports,
    Random random,
    TimeSpan boutDuration,
    Func<TimeSpan, CancellationToken, Task>? delay = null,
    int maxAttempts = 5)
{
    private const int MaxBackoffSeconds = 5;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;

    /// <summary>Reports Match started, then Bouts in order until a Participant has the majority. Resumes after completed Bouts.</summary>
    public async Task PlayAsync(Match match, Action<string> log, CancellationToken cancellationToken)
    {
        if (match.BestOf is not (1 or 3) || match.Participants is not { Count: 2 } participants)
            throw new UnsupportedMatchException("The simulation plays only head-to-head Matches with two Participants and a best-of of 1 or 3.");

        var wins = participants.ToDictionary(p => p.ParticipantId, _ => 0);
        var bout = 0;
        foreach (var completed in (match.CompletedBouts ?? []).OrderBy(b => b.Number))
        {
            bout = completed.Number;
            foreach (var result in completed.Results ?? [])
                if (result.Won && wins.ContainsKey(result.ParticipantId)) wins[result.ParticipantId]++;
        }
        var needed = match.BestOf.Value / 2 + 1;

        await WithRetryAsync(reports.ReportStartedAsync, cancellationToken);
        log("Reported Match started.");
        while (wins.Values.Max() < needed)
        {
            bout++;
            await _delay(boutDuration, cancellationToken);
            var winner = participants[random.Next(participants.Count)].ParticipantId;
            var number = bout;
            await WithRetryAsync(token => reports.ReportWinnerAsync(number, winner, token), cancellationToken);
            wins[winner]++;
            log($"Bout {bout}: Participant {winner} won.");
        }
    }

    private async Task WithRetryAsync(Func<CancellationToken, Task> report, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await report(cancellationToken);
                return;
            }
            catch (Exception error) when (attempt < maxAttempts && IsTransient(error, cancellationToken))
            {
                // Identical reports are a no-op in the API, so repeating one is safe.
                await _delay(TimeSpan.FromSeconds(Math.Min(attempt, MaxBackoffSeconds)), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception error, CancellationToken cancellationToken) => error switch
    {
        ApiException api => (int)api.StatusCode >= 500 || api.StatusCode == HttpStatusCode.RequestTimeout
            || api.StatusCode == HttpStatusCode.TooManyRequests,
        HttpRequestException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested, // HttpClient timeout
        _ => false
    };
}
