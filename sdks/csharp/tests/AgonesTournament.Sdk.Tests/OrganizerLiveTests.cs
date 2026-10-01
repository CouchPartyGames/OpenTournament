using System.Net.WebSockets;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.Organizer;

namespace AgonesTournament.Sdk.Tests;

public class OrganizerLiveTests
{
    private const string TournamentIdText = "22222222-2222-2222-2222-222222222222";
    private const string ParticipantIdText = "33333333-3333-3333-3333-333333333333";
    private const string MatchIdText = "11111111-1111-1111-1111-111111111111";
    private static readonly TournamentId TournamentId = new(Guid.Parse(TournamentIdText));
    [Fact]
    public async Task WatchesEveryMatchEndpointAndSurfacesStalledMatches()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribeAsync(socket, "organizer-token", 10, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 11, "match.changed", MatchData()), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 12, "match.changed",
                MatchData("66666666-6666-6666-6666-666666666666", "77777777-7777-7777-7777-777777777777")), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 13, "match.changed",
                MatchData(status: "stalled", allocated: false)), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot),
            _ => Task.FromResult("organizer-token"));
        await using var updates = client.WatchTournamentAsync(TournamentId, timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(10, Assert.IsType<LiveResync>(updates.Current).Sequence);
        foreach (var expectedId in new[] { MatchIdText, "66666666-6666-6666-6666-666666666666" })
        {
            Assert.True(await updates.MoveNextAsync());
            var match = Assert.IsType<MatchChanged>(Assert.IsType<LiveEvent>(updates.Current).Payload);
            Assert.Equal(new MatchId(Guid.Parse(expectedId)), match.MatchId);
            Assert.Equal("192.0.2.1", match.ServerAddress);
            Assert.Equal(7777, match.ServerPort);
        }
        Assert.True(await updates.MoveNextAsync());
        var stalled = Assert.IsType<MatchChanged>(Assert.IsType<LiveEvent>(updates.Current).Payload);
        Assert.Equal(MatchStatus.Stalled, stalled.Status);
        Assert.False(stalled.ServerAllocated);
        Assert.Null(stalled.ServerAddress);
    }

    [Fact]
    public async Task DeliversBoutsStandingsStatusAndCompletionWithSharedRanges()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribeAsync(socket, "token", 10, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 11, "bout.recorded",
                $$"""{"matchId":"{{MatchIdText}}","bouts":[{"bout":1,"results":[{"participantId":"{{ParticipantIdText}}","won":true}]}]}"""), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 12, "standings.changed",
                $$"""{"groupId":"55555555-5555-5555-5555-555555555555","complete":true,"standings":[{"participantId":"{{ParticipantIdText}}","position":1,"played":1,"wins":1,"losses":0,"points":3}]}"""), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 13, "tournament.status-changed", "{\"status\":\"completed\"}"), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 14, "tournament.completed",
                $$"""{"status":"completed","placements":[{"participantId":"{{ParticipantIdText}}","from":5,"to":8},{"participantId":"77777777-7777-7777-7777-777777777777","from":5,"to":8}]}"""), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 15, "future.event", "{\"value\":42}"), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        await using var updates = client.WatchTournamentAsync(TournamentId).GetAsyncEnumerator(timeout.Token);
        Assert.True(await updates.MoveNextAsync());
        Assert.IsType<LiveResync>(updates.Current);
        Assert.True(await updates.MoveNextAsync());
        var bout = Assert.IsType<BoutRecorded>(Assert.IsType<LiveEvent>(updates.Current).Payload);
        Assert.True(Assert.Single(Assert.Single(bout.Bouts!).Results!).Won);
        Assert.True(await updates.MoveNextAsync());
        var standings = Assert.IsType<StandingsChanged>(Assert.IsType<LiveEvent>(updates.Current).Payload);
        Assert.Equal(3, Assert.Single(standings.Standings!).Points);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(TournamentStatus.Completed, Assert.IsType<TournamentStatusChanged>(Assert.IsType<LiveEvent>(updates.Current).Payload).Status);
        Assert.True(await updates.MoveNextAsync());
        var completion = Assert.IsType<TournamentCompleted>(Assert.IsType<LiveEvent>(updates.Current).Payload);
        Assert.Equal(TournamentStatus.Completed, completion.Status);
        Assert.Equal(2, completion.Placements!.Count);
        Assert.All(completion.Placements, placement => { Assert.Equal(5, placement.From); Assert.Equal(8, placement.To); });
        Assert.True(await updates.MoveNextAsync());
        var unknown = Assert.IsType<LiveEvent>(updates.Current);
        Assert.Equal(15, unknown.Sequence);
        Assert.Equal("future.event", unknown.Type);
        Assert.Null(unknown.Payload);
        Assert.Equal(42, unknown.Data.GetProperty("value").GetInt32());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReconnectsWithFreshTokenAndResyncAfterGapOrDisconnect(bool gap)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var connections = 0;
        var tokens = 0;
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            var connection = Interlocked.Increment(ref connections);
            await AuthenticateAndSubscribeAsync(socket, "token-" + connection, connection == 1 ? 10 : 20, ct);
            if (connection == 1)
            {
                if (gap)
                    await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 12, "match.changed", MatchData()), ct);
                else await WebSocketServer.DropAsync(socket, ct);
            }
            else
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(TournamentIdText, 21, "match.changed", MatchData()), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot),
            _ => Task.FromResult("token-" + Interlocked.Increment(ref tokens)));
        await using var updates = client.WatchTournamentAsync(TournamentId, timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(10, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(20, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(21, Assert.IsType<LiveEvent>(updates.Current).Sequence);
    }

    [Fact]
    public async Task ReaderCancellationStopsWaitingForUpdates()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribeAsync(socket, "token", 10, ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        await using var updates = client.WatchTournamentAsync(TournamentId).GetAsyncEnumerator(timeout.Token);
        Assert.True(await updates.MoveNextAsync());
        timeout.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await updates.MoveNextAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EmptyTokenFailsInsteadOfSubscribingAnonymously(string? token)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (_, ct) => await Task.Delay(Timeout.Infinite, ct));
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult(token!));
        await using var updates = client.WatchTournamentAsync(TournamentId, timeout.Token).GetAsyncEnumerator();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await updates.MoveNextAsync());
    }

    [Fact]
    public async Task AuthenticationFailurePreservesProtocolError()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await WebSocketServer.ReadAsync(socket, ct);
            await WebSocketServer.SendAsync(socket, "{\"type\":\"error\",\"code\":\"unauthenticated\",\"error\":\"expired token\"}", ct);
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("expired"));
        await using var updates = client.WatchTournamentAsync(TournamentId, timeout.Token).GetAsyncEnumerator();
        var error = await Assert.ThrowsAsync<LiveProtocolException>(async () => await updates.MoveNextAsync());
        Assert.Equal("unauthenticated", error.Code);
    }

    [Fact]
    public async Task DisposingReaderClosesItsSubscription()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await AuthenticateAndSubscribeAsync(socket, "token", 10, ct);
            try { await socket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), ct); }
            finally { closed.TrySetResult(); }
        });
        using var http = new HttpClient();
        var client = new OrganizerClient(new TournamentHttpClient(http, server.ServiceRoot), _ => Task.FromResult("token"));
        await using (var updates = client.WatchTournamentAsync(TournamentId, timeout.Token).GetAsyncEnumerator())
            Assert.True(await updates.MoveNextAsync());
        await closed.Task.WaitAsync(timeout.Token);
    }
    private static async Task AuthenticateAndSubscribeAsync(WebSocket socket, string token, long sequence, CancellationToken ct)
    {
        var auth = await WebSocketServer.ReadAsync(socket, ct);
        Assert.Equal("authenticate", auth.GetProperty("type").GetString());
        Assert.Equal(token, auth.GetProperty("token").GetString());
        await WebSocketServer.SendAsync(socket, "{\"type\":\"authenticated\"}", ct);
        await WebSocketServer.SubscribeAsync(socket, TournamentIdText, sequence, ct);
    }

    private static string MatchData(string matchId = MatchIdText, string participantId = ParticipantIdText,
        string status = "allocating", bool allocated = true)
    {
        var endpoint = allocated ? ",\"serverAddress\":\"192.0.2.1\",\"serverPort\":7777" : "";
        return $$"""
            {"matchId":"{{matchId}}","stageId":"44444444-4444-4444-4444-444444444444",
             "groupId":"55555555-5555-5555-5555-555555555555","key":"R1-M1","round":1,"status":"{{status}}",
             "participants":["{{participantId}}"],"serverAllocated":{{(allocated ? "true" : "false")}},"aborts":0{{endpoint}}}
            """;
    }
}
