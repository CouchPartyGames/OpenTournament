using System.Globalization;
using System.Text.Json;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.GameClient;

/// <summary>Tournament discovery and registration. The caller owns sign-in and supplies fresh Keycloak access tokens.</summary>
public sealed partial class GameClient(TournamentHttpClient http, Func<CancellationToken, Task<string>>? accessTokenProvider = null)
{
    /// <summary>Lists Tournaments with optional Game/status filters and pagination.</summary>
    public async Task<TournamentPage> ListTournamentsAsync(string? gameId = null, TournamentStatus? status = null,
        int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 200);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        var filters = new List<string>();
        if (gameId is not null)
            filters.Add("gameId=" + Uri.EscapeDataString(gameId));
        if (status is not null)
            filters.Add("status=" + Uri.EscapeDataString(JsonSerializer.SerializeToElement(status.Value).GetString()!));
        filters.Add("limit=" + limit.ToString(CultureInfo.InvariantCulture));
        filters.Add("offset=" + offset.ToString(CultureInfo.InvariantCulture));
        return await http.ReadAsync<TournamentPage>("api/v1/tournaments?" + string.Join("&", filters),
            await TokenAsync(false, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a Tournament's settings and registration counts.</summary>
    public async Task<Tournament> TournamentAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<Tournament>(TournamentPath(tournamentId),
            await TokenAsync(false, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Reads the Tournament's Stages, Groups, Rounds, Matches, Bouts and Standings.</summary>
    public async Task<TournamentStructure> StructureAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<TournamentStructure>(TournamentPath(tournamentId) + "/structure",
            await TokenAsync(false, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Reads the Tournament's Final Placements.</summary>
    public async Task<FinalPlacements> FinalPlacementsAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<FinalPlacements>(TournamentPath(tournamentId) + "/placements",
            await TokenAsync(false, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Registers the caller's Keycloak identity by default, or a linked Player Identity. Registration during Check-in also checks in.</summary>
    public async Task<RegisteredParticipant> RegisterAsync(TournamentId tournamentId, PlayerIdentity? identity = null,
        CancellationToken cancellationToken = default)
        => await http.SendReadAsync<RegisteredParticipant>(HttpMethod.Post, TournamentPath(tournamentId) + "/participants",
            await TokenAsync(true, cancellationToken).ConfigureAwait(false),
            identity is null ? new { } : (object)new { identity }, cancellationToken).ConfigureAwait(false);

    /// <summary>Unregisters the caller's Participant before the Tournament starts.</summary>
    public async Task UnregisterAsync(TournamentId tournamentId, ParticipantId participantId, CancellationToken cancellationToken = default)
        => await http.SendAsync(HttpMethod.Delete, ParticipantPath(tournamentId, participantId),
            await TokenAsync(true, cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <summary>Checks in the caller's Participant during the Check-in Window.</summary>
    public async Task<RegisteredParticipant> CheckInAsync(TournamentId tournamentId, ParticipantId participantId,
        CancellationToken cancellationToken = default)
        => await http.SendReadAsync<RegisteredParticipant>(HttpMethod.Post, ParticipantPath(tournamentId, participantId) + "/check-in",
            await TokenAsync(true, cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <summary>Reads the caller's registrations, one for each Player Identity they own in this Tournament.</summary>
    public async Task<ParticipantRegistrations> MyRegistrationsAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<ParticipantRegistrations>(TournamentPath(tournamentId) + "/participants/me",
            await TokenAsync(true, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Reads a Match. Game Server address and port are nullable and appear only when disclosed by the API.</summary>
    public async Task<MatchDetails> MatchAsync(MatchId matchId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<MatchDetails>("api/v1/matches/" + matchId,
            await TokenAsync(false, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private static string TournamentPath(TournamentId id) => "api/v1/tournaments/" + id;
    private static string ParticipantPath(TournamentId tournamentId, ParticipantId participantId)
        => TournamentPath(tournamentId) + "/participants/" + participantId;

    private async Task<string?> TokenAsync(bool required, CancellationToken cancellationToken)
    {
        if (accessTokenProvider is null)
        {
            if (required)
                throw new InvalidOperationException("This operation requires a Keycloak access token provider.");
            return null;
        }
        var token = await accessTokenProvider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("The Keycloak access token provider returned an empty token.");
        return token;
    }
}
