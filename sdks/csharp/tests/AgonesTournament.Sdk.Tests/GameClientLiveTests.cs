using System.Net.WebSockets;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameClient;
using TournamentGameClient = AgonesTournament.Sdk.GameClient.GameClient;

namespace AgonesTournament.Sdk.Tests;

public class GameClientLiveTests
{
    private const string Tournament = "22222222-2222-2222-2222-222222222222";
    private const string Match = "11111111-1111-1111-1111-111111111111";
    private const string Participant = "33333333-3333-3333-3333-333333333333";
    private const string OtherParticipant = "77777777-7777-7777-7777-777777777777";
    private const string Group = "55555555-5555-5555-5555-555555555555";
    private const string Stage = "44444444-4444-4444-4444-444444444444";
    private static readonly TournamentId Id = new(Guid.Parse(Tournament));

    private static string MatchData(string participant = Participant, string status = "allocating", string? address = null, int aborts = 0)
        => $$"""{"matchId":"{{Match}}","stageId":"{{Stage}}","groupId":"{{Group}}","key":"R1-M1","round":1,"status":"{{status}}","participants":["{{participant}}"],"serverAllocated":{{(address is null ? "false" : "true")}},"aborts":{{aborts}}"""
            + (address is null ? "}" : ",\"serverAddress\":\"" + address + "\",\"serverPort\":7777}");

    private static string MatchDetails(string? address = null, string status = "allocating")
        => $$"""{"id":"{{Match}}","tournamentId":"{{Tournament}}","groupId":"{{Group}}","key":"R1-M1","round":1,"status":"{{status}}","participants":["{{Participant}}"],"serverAllocated":{{(address is null ? "false" : "true")}},"aborts":0,"bouts":null"""
            + (address is null ? "}" : ",\"serverAddress\":\"" + address + "\",\"serverPort\":7777}");

    private static string Structure(string status = "running")
        => $$"""{"tournamentId":"{{Tournament}}","status":"{{status}}","stages":[{"id":"{{Stage}}","position":1,"status":"running","format":"single-elimination","groups":[{"id":"{{Group}}","position":1,"status":"running","participants":null,"standings":null,"rounds":[{"round":1,"matches":[{{MatchDetails()}}]}]}]}]}""";

    private static HttpHandler Http(Func<string> match, string tournamentStatus = "running", Action? structureRead = null)
        => new((request, _) =>
        {
            Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/participants/me", StringComparison.Ordinal))
                return Task.FromResult(HttpHandler.Json($$"""{"participants":[{"id":"{{Participant}}","identity":{"kind":"keycloak","value":"player"},"status":"active","registeredAt":"2026-10-01T12:00:00Z"}]}"""));
            if (path.EndsWith("/structure", StringComparison.Ordinal))
            {
                structureRead?.Invoke();
                return Task.FromResult(HttpHandler.Json(Structure(tournamentStatus)));
            }
            Assert.EndsWith("/matches/" + Match, path);
            return Task.FromResult(HttpHandler.Json(match()));
        });

    private static async Task AuthenticateAndSubscribe(WebSocket socket, CancellationToken ct, long seq = 10)
    {
        var auth = await WebSocketServer.ReadAsync(socket, ct);
        Assert.Equal("token", auth.GetProperty("token").GetString());
        await WebSocketServer.SendAsync(socket, "{\"type\":\"authenticated\"}", ct);
        await WebSocketServer.SubscribeAsync(socket, Tournament, seq, ct);
    }

    [Fact]
    public async Task MyMatchesFiltersOtherParticipantsAndDeliversReplacementEndpointAfterAbort()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribe(socket, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 11, "match.changed", MatchData(OtherParticipant, address: "192.0.2.99")), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 12, "match.changed", MatchData(address: "192.0.2.1")), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 13, "match.changed", MatchData(status: "in-progress", address: "192.0.2.1")), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 14, "match.changed", MatchData(aborts: 1).Replace("\"aborts\":1", "\"aborts\":1,\"aborted\":true")), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 15, "match.changed", MatchData(address: "192.0.2.2", aborts: 1)), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 16, "match.changed", MatchData(status: "completed", address: "192.0.2.2", aborts: 1)), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 17, "tournament.completed", "{\"status\":\"completed\",\"placements\":[]}"), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        var reads = 0;
        using var handler = Http(() => { reads++; return MatchDetails(); });
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        var updates = new List<PlayerMatch>();
        await foreach (var update in client.MyMatchesAsync(Id, timeout.Token)) updates.Add(update);
        Assert.Equal(6, updates.Count);
        Assert.Null(updates[0].Endpoint);
        Assert.Equal("192.0.2.1", updates[1].Endpoint!.Address);
        Assert.Equal(MatchStatus.InProgress, updates[2].Status);
        Assert.Null(updates[3].Endpoint);
        Assert.Equal(1, updates[3].Aborts);
        Assert.Equal("192.0.2.2", updates[4].Endpoint!.Address);
        Assert.Equal(new MatchId(Guid.Parse(Match)), updates[4].Endpoint!.MatchId);
        Assert.Equal(7777, updates[4].Endpoint!.Port);
        Assert.Equal(MatchStatus.Completed, updates[5].Status);
        Assert.Null(updates[5].Endpoint);
        Assert.Equal(1, reads); // Private events deliver endpoints without another REST read.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HelperFindsEndpointFromLiveAllocationOrInitialMatchRead(bool alreadyAllocated)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribe(socket, ct);
            if (!alreadyAllocated)
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 11, "match.changed", MatchData(address: "192.0.2.1")), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var handler = Http(() => MatchDetails(alreadyAllocated ? "192.0.2.1" : null));
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        var endpoint = await client.WaitForNextMatchEndpointAsync(Id, timeout.Token);
        Assert.Equal("192.0.2.1", endpoint.Address);
        Assert.Equal(7777, endpoint.Port);
    }

    [Fact]
    public async Task HelperRefetchesMatchEndpointAfterASequenceGap()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var connections = 0;
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            var connection = Interlocked.Increment(ref connections);
            await AuthenticateAndSubscribe(socket, ct, connection == 1 ? 10 : 20);
            if (connection == 1)
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 12, "match.changed", MatchData()), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        var reads = 0;
        using var handler = Http(() => MatchDetails(++reads == 1 ? null : "192.0.2.2"));
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        var endpoint = await client.WaitForNextMatchEndpointAsync(Id, timeout.Token);
        Assert.Equal("192.0.2.2", endpoint.Address);
        Assert.Equal(2, reads);
    }

    [Theory]
    [InlineData("completed", false)]
    [InlineData("cancelled", false)]
    [InlineData("completed", true)]
    [InlineData("cancelled", true)]
    public async Task HelperStopsWhenTournamentEndsInSnapshotOrLiveEvent(string status, bool snapshot)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribe(socket, ct);
            if (!snapshot)
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Tournament, 11, "tournament.status-changed", "{\"status\":\"" + status + "\"}"), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var handler = Http(() => MatchDetails(), snapshot ? status : "running");
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        var error = await Assert.ThrowsAsync<TournamentEndedException>(() => client.WaitForNextMatchEndpointAsync(Id, timeout.Token));
        Assert.Equal(Id, error.TournamentId);
    }

    [Fact]
    public async Task HelperHonoursCancellationWhileWaitingForAllocation()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var snapshotRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribe(socket, ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var handler = Http(() => MatchDetails(), structureRead: () => snapshotRead.TrySetResult());
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        using var waiting = new CancellationTokenSource();
        var endpoint = client.WaitForNextMatchEndpointAsync(Id, waiting.Token);
        await snapshotRead.Task.WaitAsync(timeout.Token);
        await waiting.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => endpoint);
    }

    [Fact]
    public async Task LivePlayerOperationsRequireSignIn()
    {
        using var handler = new HttpHandler((_, _) => throw new Exception("No request expected"));
        using var http = new HttpClient(handler);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.WaitForNextMatchEndpointAsync(Id));
        Assert.Contains("Keycloak access token provider", error.Message);
    }
}
