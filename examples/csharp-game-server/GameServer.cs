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

using var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopping.Cancel(); };
using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
    context => { context.Cancel = true; stopping.Cancel(); });
using var agones = new AgonesSDK(requestTimeoutSec: 5);
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var healthFailed = false;
var health = KeepHealthyAsync();
try
{
    CheckStatus(await agones.ReadyAsync().WaitAsync(stopping.Token), "mark GameServer Ready");
    Console.WriteLine("Agones Ready; waiting for allocation.");
    // Poll the Agones state: Ready and allocation are asynchronous sidecar operations.
    while ((await agones.GetGameServerAsync().WaitAsync(stopping.Token)).Status.State != "Allocated")
        await Task.Delay(TimeSpan.FromSeconds(1), stopping.Token);

    var assignment = await new AgonesGameServer(agones).AssignmentAsync(stopping.Token);
    Console.WriteLine($"Allocated to Match {assignment.MatchId}.");
    var client = new GameServerClient(new TournamentHttpClient(http, apiUrl), assignment.MatchToken);
    try
    {
        var match = await client.MatchAsync(stopping.Token);
        if (match.MatchId != assignment.MatchId)
            throw new InvalidOperationException("The fetched Match ID does not match the Agones allocation.");
        var format = JsonSerializer.Serialize(match.Format).Trim('"');
        Console.WriteLine($"Match {match.MatchId}: Format={format}, Best-of={match.BestOf?.ToString() ?? "n/a"}, Bouts={match.Bouts?.ToString() ?? "n/a"}.");
        foreach (var participant in match.Participants ?? [])
            Console.WriteLine($"Participant {participant.ParticipantId}: Player Identity={JsonSerializer.Serialize(participant.IdentityKind)}:{JsonSerializer.Serialize(participant.IdentityValue)}, forfeited={participant.Forfeited.ToString().ToLowerInvariant()}.");
        Console.WriteLine("Roster fetched. This skeleton does not play or report Bouts; waiting for shutdown.");
    }
    catch (ApiException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
    {
        Console.Error.WriteLine("Match fetch rejected (401): the allocation's Match token is invalid, expired, or superseded. Waiting for shutdown.");
    }
    catch (ApiException error)
    {
        Console.Error.WriteLine($"Match fetch failed (HTTP {(int)error.StatusCode}). Waiting for shutdown.");
    }
    catch (HttpRequestException)
    {
        Console.Error.WriteLine("Match fetch failed: Open Tournament API is unreachable. Check OPENTOURNAMENT_API_URL and networking. Waiting for shutdown.");
    }
    catch (OperationCanceledException) when (!stopping.IsCancellationRequested)
    {
        Console.Error.WriteLine("Match fetch timed out after 10 seconds. Check API availability. Waiting for shutdown.");
    }
    // Keep the allocated server healthy and its diagnostic logs available, including on API failure.
    await Task.Delay(Timeout.InfiniteTimeSpan, stopping.Token);
}
catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
catch (Exception error)
{
    // Do not print arbitrary exception messages or response bodies: they may contain the token.
    Console.Error.WriteLine($"Game Server stopped because of {error.GetType().Name}. Check Agones connectivity and allocation metadata.");
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
        throw new InvalidOperationException($"Unable to {operation} ({status.StatusCode}).");
}
