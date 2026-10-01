using Agones;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

namespace AgonesTournament.Sdk.Examples;

public static class MatchLoop
{
    // Call once Agones has allocated this GameServer. The caller runs Agones Ready/Health as usual.
    public static async Task RunAsync(IAgonesSDK agones, Uri apiBaseUrl, IMatchGame game, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        var transport = new TournamentHttpClient(http, apiBaseUrl);
        var metadata = new AgonesGameServer(agones);
        var assignment = await metadata.AssignmentAsync(cancellationToken);
        var client = new GameServerClient(transport, assignment.MatchToken);
        using var forfeits = metadata.WatchForfeits(ids =>
        {
            foreach (var id in ids)
                game.Kick(id); // The game must marshal this to its own thread if necessary.
        });

        var match = await client.MatchAsync(cancellationToken);
        if (match.MatchId != assignment.MatchId)
            throw new InvalidOperationException("The assigned Match does not match the Match token.");
        if (match.Format == MatchFormat.FreeForAll)
            throw new ArgumentException("This example runs head-to-head Matches.");
        var winsNeeded = (match.BestOf ?? throw new InvalidOperationException("Missing Best-of.")) / 2 + 1;
        var wins = (match.CompletedBouts ?? []).SelectMany(b => b.Results ?? []).Where(r => r.Won)
            .GroupBy(r => r.ParticipantId).ToDictionary(g => g.Key, g => g.Count());
        var nextBout = (match.CompletedBouts ?? []).Select(b => b.Number).DefaultIfEmpty(0).Max() + 1;
        await client.ReportStartedAsync(cancellationToken);
        for (var bout = nextBout; !wins.Values.Any(count => count >= winsNeeded); bout++)
        {
            var noShows = await game.WaitForParticipantsAsync(match, cancellationToken);
            ParticipantId winner;
            if (noShows.Count != 0)
            {
                await client.ReportNoShowsAsync(bout, noShows, cancellationToken);
                if (noShows.Count == 2) // A double Forfeit decides the Match immediately.
                    break;
                winner = (match.Participants ?? []).Single(p => !noShows.Contains(p.ParticipantId)).ParticipantId;
            }
            else
            {
                winner = await game.PlayHeadToHeadAsync(match, bout, cancellationToken);
                await client.ReportWinnerAsync(bout, winner, cancellationToken);
            }
            wins[winner] = wins.GetValueOrDefault(winner) + 1;
            // The API rejects the token once the Match completes; stop after the deciding report.
            if (wins[winner] < winsNeeded)
                match = await client.MatchAsync(cancellationToken);
        }
        await agones.ShutDownAsync();
    }
}

// Implement these with the game's networking and scoring. Skip forfeited Participants when waiting or playing.
public interface IMatchGame
{
    void Kick(ParticipantId participantId);
    Task<IReadOnlyList<ParticipantId>> WaitForParticipantsAsync(Match match, CancellationToken cancellationToken);
    Task<ParticipantId> PlayHeadToHeadAsync(Match match, int bout, CancellationToken cancellationToken);
}
