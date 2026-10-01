#:project ../../sdks/csharp/src/AgonesTournament.Sdk.GameServer/AgonesTournament.Sdk.GameServer.csproj
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property TreatWarningsAsErrors=true
// The SDK uses reflection-based JSON serialization and gRPC; disable file-app AOT.
#:property PublishAot=false

using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using Agones;
using AgonesTournament.ReferenceGameServer;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;
using Grpc.Core;

var configuredUrl = Environment.GetEnvironmentVariable("OPENTOURNAMENT_API_URL");
if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var apiUrl)
    || (apiUrl.Scheme != "http" && apiUrl.Scheme != "https")
    || !string.IsNullOrEmpty(apiUrl.UserInfo) || !string.IsNullOrEmpty(apiUrl.Query)
    || !string.IsNullOrEmpty(apiUrl.Fragment))
{
    Console.Error.WriteLine("Set OPENTOURNAMENT_API_URL to the HTTP(S) service root URL, e.g. http://localhost:8080.");
    return 1;
}

var boutSeconds = 5.0;
var configuredBout = Environment.GetEnvironmentVariable("BOUT_DURATION_SECONDS");
if (!string.IsNullOrEmpty(configuredBout)
    && (!double.TryParse(configuredBout, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out boutSeconds)
        || boutSeconds < 0 || boutSeconds > 3600))
{
    Console.Error.WriteLine("Set BOUT_DURATION_SECONDS to a number of seconds between 0 and 3600.");
    return 1;
}

using var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopping.Cancel(); };
using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
    context => { context.Cancel = true; stopping.Cancel(); });
using var agones = new AgonesSDK(requestTimeoutSec: 5);
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var healthFailed = false;
var health = KeepHealthyAsync();
var operation = "mark GameServer Ready";
try
{
    CheckStatus(await agones.ReadyAsync().WaitAsync(stopping.Token), "mark GameServer Ready");
    Console.WriteLine("Agones Ready; waiting for allocation.");
    // Poll the Agones state: Ready and allocation are asynchronous sidecar operations.
    operation = "wait for Agones allocation";
    while ((await agones.GetGameServerAsync().WaitAsync(stopping.Token)).Status.State != "Allocated")
        await Task.Delay(TimeSpan.FromSeconds(1), stopping.Token);

    operation = "read allocation metadata";
    MatchAssignment assignment;
    try
    {
        assignment = await new AgonesGameServer(agones).AssignmentAsync(stopping.Token);
    }
    catch (InvalidOperationException error)
    {
        // AssignmentAsync validation messages name only the missing label/annotation, never its value.
        Console.Error.WriteLine(error.Message);
        return 1;
    }
    Console.WriteLine($"Allocated to Match {assignment.MatchId}.");
    var client = new GameServerClient(new TournamentHttpClient(http, apiUrl), assignment.MatchToken);
    operation = "fetch and log Match roster";
    try
    {
        var match = await client.MatchAsync(stopping.Token);
        if (match.MatchId != assignment.MatchId)
        {
            Console.Error.WriteLine("The fetched Match ID does not match the Agones allocation.");
            return 1;
        }
        var format = JsonSerializer.Serialize(match.Format).Trim('"');
        Console.WriteLine($"Match {match.MatchId}: Format={format}, Best-of={match.BestOf?.ToString() ?? "n/a"}, Bouts={match.Bouts?.ToString() ?? "n/a"}.");
        foreach (var participant in match.Participants ?? [])
            Console.WriteLine($"Participant {participant.ParticipantId}: Player Identity={JsonSerializer.Serialize(participant.IdentityKind)}:{JsonSerializer.Serialize(participant.IdentityValue)}, forfeited={participant.Forfeited.ToString().ToLowerInvariant()}.");
        operation = "play the Match";
        await new MatchSimulation(new Reports(client), Random.Shared, TimeSpan.FromSeconds(boutSeconds))
            .PlayAsync(match, Console.WriteLine, stopping.Token);
        operation = "shut down the GameServer";
        CheckStatus(await agones.ShutDownAsync().WaitAsync(stopping.Token), "shut down the GameServer");
        Console.WriteLine("Match decided; shut down through Agones.");
        return 0;
    }
    catch (InvalidOperationException error) when (error.Message.StartsWith("The simulation", StringComparison.Ordinal))
    {
        Console.Error.WriteLine(error.Message + " Waiting for shutdown.");
    }
    catch (ApiException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
    {
        Console.Error.WriteLine("Match request rejected (401): the allocation's Match token is invalid, expired, or superseded. Waiting for shutdown.");
    }
    catch (ApiException error)
    {
        Console.Error.WriteLine($"Match request failed (HTTP {(int)error.StatusCode}). Waiting for shutdown.");
    }
    catch (HttpRequestException)
    {
        Console.Error.WriteLine("Match request failed: Open Tournament API is unreachable. Check OPENTOURNAMENT_API_URL and networking. Waiting for shutdown.");
    }
    catch (OperationCanceledException) when (!stopping.IsCancellationRequested)
    {
        Console.Error.WriteLine("Match request timed out after 10 seconds. Check API availability. Waiting for shutdown.");
    }
    // Keep the allocated server healthy and its diagnostic logs available, including on API failure.
    await Task.Delay(Timeout.InfiniteTimeSpan, stopping.Token);
}
catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
catch (Exception error)
{
    // Do not print arbitrary exception messages or response bodies: they may contain the token.
    var status = error is RpcException rpc ? $", gRPC {rpc.StatusCode}" : "";
    Console.Error.WriteLine($"Cannot {operation}: {error.GetType().Name}{status}.");
    return 1;
}
finally
{
    stopping.Cancel();
    await health;
}
return healthFailed ? 1 : 0;

async Task KeepHealthyAsync()
{
    try
    {
        while (!stopping.IsCancellationRequested)
        {
            CheckStatus(await agones.HealthAsync().WaitAsync(stopping.Token), "send Agones health ping");
            await Task.Delay(TimeSpan.FromSeconds(2), stopping.Token);
        }
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    catch (Exception error)
    {
        Console.Error.WriteLine($"Agones health failed ({error.GetType().Name}); stopping the Game Server.");
        healthFailed = true;
        stopping.Cancel();
    }
}

static void CheckStatus(Status status, string operation)
{
    if (status.StatusCode != StatusCode.OK)
    {
        Console.Error.WriteLine($"Unable to {operation}: gRPC {status.StatusCode}.");
        throw new InvalidOperationException("Agones operation failed.");
    }
}

sealed class Reports(GameServerClient client) : IMatchReports
{
    public Task ReportStartedAsync(CancellationToken cancellationToken) => client.ReportStartedAsync(cancellationToken);
    public Task ReportWinnerAsync(int bout, ParticipantId winner, CancellationToken cancellationToken)
        => client.ReportWinnerAsync(bout, winner, cancellationToken);
}
