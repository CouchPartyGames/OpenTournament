using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.Tests;

public class LiveConnectionTests
{
    private const string Id = "22222222-2222-2222-2222-222222222222";
    private const string Participant = "33333333-3333-3333-3333-333333333333";

    [Fact]
    public async Task CompletionPlacementsFollowTheLiveProtocolWithoutRestIdentityFields()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await WebSocketServer.SubscribeAsync(socket, Id, 10, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 11, "tournament.completed",
                $$"""{"status":"completed","placements":[{"participantId":"{{Participant}}","from":1,"to":1}]}"""), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        await using var live = new LiveConnection(server.ServiceRoot);
        live.Subscribe(new TournamentId(Guid.Parse(Id)));
        await using var updates = live.NotificationsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.IsType<LiveResync>(updates.Current);
        Assert.True(await updates.MoveNextAsync());
        var completed = Assert.IsType<TournamentCompleted>(Assert.IsType<LiveEvent>(updates.Current).Payload);
        Assert.Equal(1, Assert.Single(completed.Placements!).From);
    }

    [Theory]
    [InlineData("tournament.status-changed", "{\"status\":\"running\"}", typeof(TournamentStatusChanged))]
    [InlineData("registrations.changed", "{\"registered\":3,\"checkedIn\":2}", typeof(RegistrationsChanged))]
    [InlineData("participant.changed", "{\"participantId\":\"33333333-3333-3333-3333-333333333333\",\"status\":\"active\"}", typeof(ParticipantChanged))]
    [InlineData("stage.started", "{\"stageId\":\"44444444-4444-4444-4444-444444444444\",\"position\":1}", typeof(StageStarted))]
    [InlineData("stage.completed", "{\"stageId\":\"44444444-4444-4444-4444-444444444444\",\"position\":1}", typeof(StageCompleted))]
    [InlineData("match.changed", "{\"matchId\":\"11111111-1111-1111-1111-111111111111\",\"stageId\":\"44444444-4444-4444-4444-444444444444\",\"groupId\":\"55555555-5555-5555-5555-555555555555\",\"key\":\"R1-M1\",\"round\":1,\"status\":\"allocating\",\"participants\":[],\"serverAllocated\":true,\"aborts\":1,\"serverAddress\":\"192.0.2.1\",\"serverPort\":7777}", typeof(MatchChanged))]
    [InlineData("bout.recorded", "{\"matchId\":\"11111111-1111-1111-1111-111111111111\",\"bouts\":[{\"bout\":1,\"results\":null}]}", typeof(BoutRecorded))]
    [InlineData("standings.changed", "{\"groupId\":\"55555555-5555-5555-5555-555555555555\",\"complete\":true,\"standings\":[]}", typeof(StandingsChanged))]
    [InlineData("future.event", "{\"newField\":42}", null)]
    public async Task AuthenticatedEventsRetainTypedAndUnknownPayloads(string type, string data, Type? payloadType)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            var auth = await WebSocketServer.ReadAsync(socket, ct);
            Assert.Equal("authenticate", auth.GetProperty("type").GetString());
            Assert.Equal("fresh-token", auth.GetProperty("token").GetString());
            await WebSocketServer.SendAsync(socket, "{\"type\":\"authenticated\"}", ct);
            await WebSocketServer.SubscribeAsync(socket, Id, 10, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 11, type, data), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        await using var live = new LiveConnection(server.ServiceRoot, _ => Task.FromResult("fresh-token"));
        live.Subscribe(new TournamentId(Guid.Parse(Id)));
        await using var updates = live.NotificationsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(10, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        var update = Assert.IsType<LiveEvent>(updates.Current);
        Assert.Equal(11, update.Sequence);
        Assert.Equal(type, update.Type);
        Assert.Equal(data, update.Data.GetRawText());
        Assert.Equal(payloadType, update.Payload?.GetType());
        if (update.Payload is MatchChanged match)
        {
            Assert.Equal("192.0.2.1", match.ServerAddress);
            Assert.Equal(7777, match.ServerPort);
            Assert.Equal(MatchStatus.Allocating, match.Status);
        }
        if (update.Payload is BoutRecorded bout)
            Assert.Equal(1, Assert.Single(bout.Bouts!).Number);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GapOrDisconnectReconnectsWithFreshAuthenticationAndNewBaseline(bool gap)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var connections = 0;
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            var connection = Interlocked.Increment(ref connections);
            var auth = await WebSocketServer.ReadAsync(socket, ct);
            Assert.Equal("token-" + connection, auth.GetProperty("token").GetString());
            await WebSocketServer.SendAsync(socket, "{\"type\":\"authenticated\"}", ct);
            await WebSocketServer.SubscribeAsync(socket, Id, connection == 1 ? 10 : 20, ct);
            if (connection == 1)
            {
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 10, "future", "{}"), ct);
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 11, "future", "{}"), ct);
                await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 11, "future", "{}"), ct);
                if (gap)
                    await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 13, "future", "{}"), ct);
                else socket.Abort();
            }
            else await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 21, "future", "{}"), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        var tokens = 0;
        await using var live = new LiveConnection(server.ServiceRoot, _ => Task.FromResult("token-" + Interlocked.Increment(ref tokens)));
        live.Subscribe(new TournamentId(Guid.Parse(Id)));
        await using var updates = live.NotificationsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(10, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(11, Assert.IsType<LiveEvent>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(20, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(21, Assert.IsType<LiveEvent>(updates.Current).Sequence);
    }

    [Fact]
    public async Task SubscriptionsTrackIndependentSequencesAndUnsubscribeOnTheWire()
    {
        const string second = "66666666-6666-6666-6666-666666666666";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await WebSocketServer.SubscribeAsync(socket, Id, 10, ct);
            await WebSocketServer.SubscribeAsync(socket, second, 50, ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(second, 51, "future", "{}"), ct);
            var unsubscribe = await WebSocketServer.ReadAsync(socket, ct);
            Assert.Equal("unsubscribe", unsubscribe.GetProperty("type").GetString());
            Assert.Equal(second, unsubscribe.GetProperty("tournamentId").GetString());
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(second, 52, "future", "{}"), ct);
            await WebSocketServer.SendAsync(socket, WebSocketServer.Event(Id, 11, "future", "{}"), ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        await using var live = new LiveConnection(server.ServiceRoot);
        live.Subscribe(new TournamentId(Guid.Parse(Id)));
        live.Subscribe(new TournamentId(Guid.Parse(second)));
        await using var updates = live.NotificationsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await updates.MoveNextAsync());
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(50, Assert.IsType<LiveResync>(updates.Current).Sequence);
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(51, Assert.IsType<LiveEvent>(updates.Current).Sequence);
        live.Unsubscribe(new TournamentId(Guid.Parse(second)));
        Assert.True(await updates.MoveNextAsync());
        Assert.Equal(11, Assert.IsType<LiveEvent>(updates.Current).Sequence);
    }

    [Fact]
    public async Task AuthenticationErrorsSurfaceTheirProtocolCode()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = await WebSocketServer.StartAsync(async (socket, ct) =>
        {
            await WebSocketServer.ReadAsync(socket, ct);
            await WebSocketServer.SendAsync(socket, "{\"type\":\"error\",\"code\":\"unauthenticated\",\"error\":\"expired token\"}", ct);
        });
        await using var live = new LiveConnection(server.ServiceRoot, _ => Task.FromResult("expired"));
        live.Subscribe(new TournamentId(Guid.Parse(Id)));
        await using var updates = live.NotificationsAsync(timeout.Token).GetAsyncEnumerator();
        var error = await Assert.ThrowsAsync<LiveProtocolException>(async () => await updates.MoveNextAsync());
        Assert.Equal("unauthenticated", error.Code);
    }
}
