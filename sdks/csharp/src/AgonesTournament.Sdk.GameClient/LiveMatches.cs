using System.Runtime.CompilerServices;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.GameClient;

/// <summary>An allocated Game Server to which the player can connect.</summary>
public sealed record GameServerEndpoint(MatchId MatchId, string Address, int Port);

/// <summary>The signed-in player's Match state, from a snapshot or a live event.</summary>
public sealed record PlayerMatch(MatchId MatchId, MatchStatus Status, bool ServerAllocated, int Aborts,
    GameServerEndpoint? Endpoint);

/// <summary>The Tournament ended before a next Match endpoint was available.</summary>
public sealed class TournamentEndedException(TournamentId tournamentId) : InvalidOperationException("The Tournament ended before a Match endpoint was available.")
{
    /// <summary>The completed or cancelled Tournament.</summary>
    public TournamentId TournamentId { get; } = tournamentId;
}

public sealed partial class GameClient
{
    /// <summary>Streams the player's Matches, including replacement endpoints after an Abort. Ends when the Tournament completes or is cancelled.</summary>
    public async IAsyncEnumerable<PlayerMatch> MyMatchesAsync(TournamentId tournamentId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (accessTokenProvider is null)
            throw new InvalidOperationException("This operation requires a Keycloak access token provider.");
        await using var live = http.CreateLiveConnection(accessTokenProvider);
        live.Subscribe(tournamentId);
        var participants = new HashSet<ParticipantId>();
        var known = new Dictionary<MatchId, PlayerMatch>();
        await foreach (var notification in live.NotificationsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (notification is LiveResync || notification is LiveEvent { Payload: RegistrationsChanged })
            {
                var registrations = await MyRegistrationsAsync(tournamentId, cancellationToken).ConfigureAwait(false);
                participants = (registrations.Participants ?? []).Select(p => p.Id).ToHashSet();
                var structure = await StructureAsync(tournamentId, cancellationToken).ConfigureAwait(false);
                if (Ended(structure.Status)) yield break;
                var matches = (structure.Stages ?? []).SelectMany(s => s.Groups ?? [])
                    .SelectMany(g => g.Rounds ?? []).SelectMany(r => r.Matches ?? [])
                    .Where(m => (m.Participants ?? []).Any(participants.Contains));
                foreach (var match in matches)
                {
                    var details = await MatchAsync(match.Id, cancellationToken).ConfigureAwait(false);
                    var update = new PlayerMatch(details.Id, details.Status, details.ServerAllocated, details.Aborts,
                        Endpoint(details.Id, details.Status, details.ServerAllocated, details.ServerAddress, details.ServerPort));
                    if (Remember(known, update)) yield return update;
                }
            }
            else if (notification is LiveEvent { Payload: TournamentCompleted }
                || notification is LiveEvent { Payload: TournamentStatusChanged status } && Ended(status.Status))
                yield break;
            else if (notification is LiveEvent { Payload: MatchChanged match }
                && (match.Participants ?? []).Any(participants.Contains))
            {
                var update = new PlayerMatch(match.MatchId, match.Status, match.ServerAllocated, match.Aborts,
                    Endpoint(match.MatchId, match.Status, match.ServerAllocated, match.ServerAddress, match.ServerPort));
                if (Remember(known, update)) yield return update;
            }
        }
    }

    /// <summary>Waits for the next allocated endpoint, including one already allocated. Throws TournamentEndedException if the Tournament ends first.</summary>
    public async Task<GameServerEndpoint> WaitForNextMatchEndpointAsync(TournamentId tournamentId,
        CancellationToken cancellationToken = default)
    {
        await foreach (var match in MyMatchesAsync(tournamentId, cancellationToken).ConfigureAwait(false))
            if (match.Endpoint is not null) return match.Endpoint;
        throw new TournamentEndedException(tournamentId);
    }

    private static bool Remember(Dictionary<MatchId, PlayerMatch> known, PlayerMatch update)
    {
        if (known.TryGetValue(update.MatchId, out var previous))
        {
            if (previous == update || IsOlder(update, previous)) return false;
        }
        known[update.MatchId] = update;
        return true;
    }

    private static bool IsOlder(PlayerMatch update, PlayerMatch previous)
    {
        // REST has no sequence watermark and can already include buffered events.
        // An Abort is the only way to return a Match to allocation or replace its server.
        if (Terminal(previous.Status)) return true;
        if (update.Aborts != previous.Aborts) return update.Aborts < previous.Aborts;
        if (Progress(update.Status) < Progress(previous.Status)) return true;
        // Allocation cannot disappear within one playing status/generation. A Stalled
        // Match releases its server at the Result Deadline without recording an Abort.
        return previous.ServerAllocated && !update.ServerAllocated && update.Status == previous.Status
            && Connectable(update.Status);
    }

    private static bool Connectable(MatchStatus status) => status is MatchStatus.Allocating or MatchStatus.InProgress;

    private static bool Terminal(MatchStatus status) => status is MatchStatus.Completed or MatchStatus.Cancelled;

    private static int Progress(MatchStatus status) => status switch
    {
        MatchStatus.Pending => 0,
        MatchStatus.Ready => 1,
        MatchStatus.Allocating => 2,
        MatchStatus.InProgress => 3,
        MatchStatus.Stalled => 4,
        MatchStatus.Completed or MatchStatus.Cancelled => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static bool Ended(TournamentStatus status) => status is TournamentStatus.Completed or TournamentStatus.Cancelled;

    private static GameServerEndpoint? Endpoint(MatchId matchId, MatchStatus status, bool allocated, string? address, int? port)
        => allocated && Connectable(status)
            && !string.IsNullOrWhiteSpace(address) && port is > 0
                ? new GameServerEndpoint(matchId, address, port.Value) : null;
}
