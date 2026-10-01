using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.GameServer;

/// <summary>Match lifecycle operations authenticated with one allocation's Match token.</summary>
public sealed class GameServerClient(TournamentHttpClient http, string matchToken)
{
    private const string MatchPath = "api/v1/game-server/match";

    /// <summary>Fetches the assigned Match and completed Bouts for resuming after an Abort.</summary>
    public Task<Match> MatchAsync(CancellationToken cancellationToken = default)
        => http.ReadAsync<Match>(MatchPath, matchToken, cancellationToken);
    /// <summary>Reports that the Match has started.</summary>
    public Task ReportStartedAsync(CancellationToken cancellationToken = default)
        => http.SendAsync(HttpMethod.Post, MatchPath + "/started", matchToken, cancellationToken: cancellationToken);

    /// <summary>Reports a head-to-head Bout winner. Identical retries succeed; different results throw BoutConflictException.</summary>
    public Task ReportWinnerAsync(int bout, ParticipantId winner, CancellationToken cancellationToken = default)
        => http.SendAsync(HttpMethod.Put, BoutPath(bout), matchToken, new { winner }, cancellationToken);

    /// <summary>Reports placements and points for every Participant still playing in a free-for-all Bout.</summary>
    public Task ReportPlacementsAsync(int bout, IReadOnlyList<BoutPlacement> placements, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);
        return http.SendAsync(HttpMethod.Put, BoutPath(bout), matchToken, new { placements }, cancellationToken);
    }

    /// <summary>Reports Participants who did not turn up for a Bout.</summary>
    public Task ReportNoShowsAsync(int bout, IReadOnlyList<ParticipantId> participantIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participantIds);
        return http.SendAsync(HttpMethod.Post, BoutPath(bout) + "/no-shows", matchToken, new { participantIds }, cancellationToken);
    }

    private static string BoutPath(int bout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bout, 1);
        return MatchPath + "/bouts/" + bout.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
