using System.Threading.Channels;
using Agones;
using Agones.Dev.Sdk;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;
using AgonesServer = Agones.Dev.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

public class AgonesGameServerTests
{
    [Fact]
    public async Task ReadsMatchIdAndTokenThroughTheAgonesSdkWithoutExposingTokenInToString()
    {
        var snapshot = new AgonesServer { ObjectMeta = new AgonesServer.Types.ObjectMeta() };
        snapshot.ObjectMeta.Labels["opentournament/match-id"] = "11111111-1111-1111-1111-111111111111";
        snapshot.ObjectMeta.Annotations["opentournament/match-token"] = "secret-token";
        var fakeAgones = new AgonesTransport(snapshot);
        using var agones = new AgonesSDK(sdkClient: new SDK.SDKClient(fakeAgones));
        var integration = new AgonesGameServer(agones);
        var assignment = await integration.AssignmentAsync();
        Assert.Equal(new MatchId(Guid.Parse("11111111-1111-1111-1111-111111111111")), assignment.MatchId);
        Assert.Equal("secret-token", assignment.MatchToken);
        Assert.DoesNotContain("secret-token", assignment.ToString());
    }

    [Fact]
    public async Task ReportsOnlyNewForfeitsAcrossUnrelatedUpdatesDuplicatesAndReorderedIds()
    {
        var fakeAgones = new AgonesTransport(new AgonesServer());
        using var agones = new AgonesSDK(sdkClient: new SDK.SDKClient(fakeAgones));
        var integration = new AgonesGameServer(agones);
        var notifications = Channel.CreateUnbounded<IReadOnlyList<ParticipantId>>();
        using var subscription = integration.WatchForfeits(ids => notifications.Writer.TryWrite(ids));
        var observed = Channel.CreateUnbounded<string>();
        agones.WatchGameServer(snapshot => observed.Writer.TryWrite(snapshot.ObjectMeta.Annotations["opentournament/forfeited"]));
        var first = "11111111-1111-1111-1111-111111111111";
        var second = "22222222-2222-2222-2222-222222222222";
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(first));
        Assert.Equal(new ParticipantId(Guid.Parse(first)), Assert.Single(await notifications.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5))));
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(first));
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(first + "," + first));
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(second + "," + first));
        Assert.Equal(new ParticipantId(Guid.Parse(second)), Assert.Single(await notifications.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5))));
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(first + "," + second));
        var third = "33333333-3333-3333-3333-333333333333";
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(first + "," + second + "," + third));
        Assert.Equal(new ParticipantId(Guid.Parse(third)), Assert.Single(await notifications.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5))));
        subscription.Dispose();
        var afterDisposal = "44444444-4444-4444-4444-444444444444";
        await fakeAgones.Updates.Writer.WriteAsync(Snapshot(afterDisposal));
        while (await observed.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)) != afterDisposal) { }
        Assert.False(notifications.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(null, "secret-token")]
    [InlineData("invalid", "secret-token")]
    [InlineData("00000000-0000-0000-0000-000000000000", "secret-token")]
    [InlineData("11111111-1111-1111-1111-111111111111", null)]
    [InlineData("11111111-1111-1111-1111-111111111111", " ")]
    public async Task MissingOrInvalidAllocationMetadataFailsWithoutExposingSecrets(string? matchId, string? matchToken)
    {
        var snapshot = new AgonesServer { ObjectMeta = new AgonesServer.Types.ObjectMeta() };
        if (matchId is not null)
            snapshot.ObjectMeta.Labels["opentournament/match-id"] = matchId;
        if (matchToken is not null)
            snapshot.ObjectMeta.Annotations["opentournament/match-token"] = matchToken;
        using var agones = new AgonesSDK(sdkClient: new SDK.SDKClient(new AgonesTransport(snapshot)));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new AgonesGameServer(agones).AssignmentAsync());
        Assert.DoesNotContain("secret-token", error.ToString());
    }

    private static AgonesServer Snapshot(string forfeited)
    {
        var snapshot = new AgonesServer { ObjectMeta = new AgonesServer.Types.ObjectMeta() };
        snapshot.ObjectMeta.Annotations["opentournament/forfeited"] = forfeited;
        return snapshot;
    }
}
