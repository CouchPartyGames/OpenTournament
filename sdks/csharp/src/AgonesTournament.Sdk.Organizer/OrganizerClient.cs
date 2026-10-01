using System.Globalization;
using System.Text.Json;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.Organizer;

/// <summary>Tournament management for Organizers and trusted Game backends. The caller owns token acquisition and supplies a fresh access token per call.</summary>
public sealed class OrganizerClient(TournamentHttpClient http, Func<CancellationToken, Task<string>> accessTokenProvider)
{
    /// <summary>Creates a Tournament. Service clients must be trusted for the Game; invalid settings throw ValidationException.</summary>
    public async Task<Tournament> CreateTournamentAsync(NewTournament tournament, CancellationToken cancellationToken = default)
        => await http.SendReadAsync<Tournament>(HttpMethod.Post, "api/v1/tournaments",
            await TokenAsync(cancellationToken).ConfigureAwait(false), tournament, cancellationToken).ConfigureAwait(false);

    /// <summary>Replaces all Draft settings. SettingsFrozenException signals opened registration; DeclaredInGitException requires editing the Manifest.</summary>
    public async Task<Tournament> EditTournamentAsync(TournamentId tournamentId, TournamentSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Serialize as settings even when a caller reuses a NewTournament: Game is immutable.
        var body = JsonSerializer.SerializeToElement<TournamentSettings>(settings, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return await http.SendReadAsync<Tournament>(HttpMethod.Put, TournamentPath(tournamentId),
            await TokenAsync(cancellationToken).ConfigureAwait(false), body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a Tournament's settings and registration counts.</summary>
    public async Task<Tournament> TournamentAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<Tournament>(TournamentPath(tournamentId),
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Lists Tournaments with optional Game/status filters, limit 1–200 and nonnegative offset.</summary>
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
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a Tournament, including a Declared Tournament, and returns its updated state.</summary>
    public async Task<Tournament> CancelTournamentAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.SendReadAsync<Tournament>(HttpMethod.Post, TournamentPath(tournamentId) + "/cancel",
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <summary>Lists all Participants in a Tournament; the response collection may be null.</summary>
    public async Task<ParticipantRegistrations> ListParticipantsAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
        => await http.ReadAsync<ParticipantRegistrations>(TournamentPath(tournamentId) + "/participants",
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Registers an explicit Player Identity. Acting on another player's behalf requires a trusted Game backend token. Registration during Check-in also checks in.</summary>
    public async Task<RegisteredParticipant> RegisterAsync(TournamentId tournamentId, PlayerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return await http.SendReadAsync<RegisteredParticipant>(HttpMethod.Post, TournamentPath(tournamentId) + "/participants",
            await TokenAsync(cancellationToken).ConfigureAwait(false), new { identity }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Checks in a Participant during the Check-in Window. Acting for another player requires a trusted Game backend token, even for the Organizer.</summary>
    public async Task<RegisteredParticipant> CheckInAsync(TournamentId tournamentId, ParticipantId participantId,
        CancellationToken cancellationToken = default)
        => await http.SendReadAsync<RegisteredParticipant>(HttpMethod.Post, ParticipantPath(tournamentId, participantId) + "/check-in",
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <summary>Disqualifies a Participant from a running Tournament, forfeiting their unfinished Bouts.</summary>
    public async Task DisqualifyAsync(TournamentId tournamentId, ParticipantId participantId, CancellationToken cancellationToken = default)
        => await http.SendAsync(HttpMethod.Post, ParticipantPath(tournamentId, participantId) + "/disqualify",
            await TokenAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);

    private static string ParticipantPath(TournamentId tournamentId, ParticipantId participantId)
        => TournamentPath(tournamentId) + "/participants/" + participantId;

    private static string TournamentPath(TournamentId id) => "api/v1/tournaments/" + id;

    private async Task<string> TokenAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessTokenProvider);
        var token = await accessTokenProvider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("The access token provider returned an empty token.");
        return token;
    }
}
