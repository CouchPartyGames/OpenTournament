using Agones;
using AgonesTournament.Sdk.Core;
using AgonesServer = Agones.Dev.Sdk.GameServer;

namespace AgonesTournament.Sdk.GameServer;

/// <summary>An allocation's credentials. MatchToken is a secret and is excluded from ToString.</summary>
public sealed class MatchAssignment(MatchId matchId, string matchToken)
{
    public MatchId MatchId { get; } = matchId;
    public string MatchToken { get; } = matchToken;
    public override string ToString() => $"Match {MatchId}";
}

/// <summary>Reads Tournament metadata through the Agones C# SDK. The caller owns and disposes the SDK.</summary>
public sealed class AgonesGameServer(IAgonesSDK agones)
{
    public const string MatchIdLabel = "opentournament/match-id";
    public const string MatchTokenAnnotation = "opentournament/match-token";
    public const string ForfeitedAnnotation = "opentournament/forfeited";

    /// <summary>Reads credentials after allocation. Cancellation stops waiting; the Agones SDK controls the RPC lifetime.</summary>
    public async Task<MatchAssignment> AssignmentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var server = await agones.GetGameServerAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        if (server.ObjectMeta is null
            || !server.ObjectMeta.Labels.TryGetValue(MatchIdLabel, out var matchId)
            || !Guid.TryParse(matchId, out var id) || id == Guid.Empty)
            throw new InvalidOperationException($"The allocated GameServer must have a valid {MatchIdLabel} label.");
        if (!server.ObjectMeta.Annotations.TryGetValue(MatchTokenAnnotation, out var token)
            || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"The allocated GameServer must have a {MatchTokenAnnotation} annotation.");
        return new MatchAssignment(new MatchId(id), token);
    }

    /// <summary>
    /// Calls back with newly forfeited Participant IDs, including any in the first snapshot.
    /// Callbacks run on Agones' watch thread and must return promptly. Disposing suppresses further callbacks;
    /// Agones retains the registered delegate until the caller disposes its SDK.
    /// </summary>
    public IDisposable WatchForfeits(Action<IReadOnlyList<ParticipantId>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var subscription = new ForfeitSubscription(callback);
        agones.WatchGameServer(subscription.Update);
        return subscription;
    }

    private sealed class ForfeitSubscription(Action<IReadOnlyList<ParticipantId>> callback) : IDisposable
    {
        private readonly object sync = new();
        private readonly HashSet<ParticipantId> seen = [];
        private Action<IReadOnlyList<ParticipantId>>? notify = callback;

        internal void Update(AgonesServer server)
        {
            lock (sync)
            {
                if (notify is null || server.ObjectMeta is null
                    || !server.ObjectMeta.Annotations.TryGetValue(ForfeitedAnnotation, out var annotation))
                    return;
                var ids = annotation.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => new ParticipantId(Guid.Parse(value))).ToArray();
                var newlyForfeited = ids.Where(seen.Add).ToArray();
                if (newlyForfeited.Length != 0)
                    notify(newlyForfeited);
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                notify = null;
                seen.Clear();
            }
        }
    }
}
