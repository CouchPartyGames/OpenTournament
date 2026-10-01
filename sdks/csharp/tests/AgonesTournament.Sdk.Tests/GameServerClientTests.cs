using System.Net;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

public class GameServerClientTests
{
    [Fact]
    public async Task FetchesAssignedMatchWithIdentitiesAndCompletedBoutsAfterAbort()
    {
        using var fakeHttp = new HttpHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://tournament.example/api/v1/game-server/match", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer match-token", request.Headers.Authorization!.ToString());
            return Task.FromResult(HttpHandler.Json("""
                {"matchId":"11111111-1111-1111-1111-111111111111",
                 "tournamentId":"22222222-2222-2222-2222-222222222222",
                 "gameId":"marbles","format":"single-elimination","status":"in-progress","bestOf":3,
                 "participants":[{"participantId":"33333333-3333-3333-3333-333333333333",
                   "identityKind":"steam","identityValue":"external-id","forfeited":true}],
                 "completedBouts":[{"bout":1,"results":[{"participantId":"33333333-3333-3333-3333-333333333333","won":true}]}]}
                """));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")), "match-token");
        var match = await client.MatchAsync();
        Assert.Equal(new MatchId(Guid.Parse("11111111-1111-1111-1111-111111111111")), match.MatchId);
        Assert.Equal(MatchFormat.SingleElimination, match.Format);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Equal(3, match.BestOf);
        Assert.Null(match.Bouts);
        var participant = Assert.Single(match.Participants!);
        Assert.Equal("steam", participant.IdentityKind);
        Assert.Equal("external-id", participant.IdentityValue);
        Assert.True(participant.Forfeited);
        Assert.True(Assert.Single(Assert.Single(match.CompletedBouts!).Results!).Won);
    }
    [Fact]
    public async Task ReportsStartedWinnerPlacementsAndNoShowsUsingTheAssignedToken()
    {
        var requests = new List<(string Method, string Path, string? Body)>();
        using var fakeHttp = new HttpHandler(async (request, cancellationToken) =>
        {
            Assert.Equal("Bearer allocation-token", request.Headers.Authorization!.ToString());
            if (request.Content is not null)
                Assert.Equal("application/json", request.Content.Headers.ContentType!.MediaType);
            requests.Add((request.Method.Method, request.RequestUri!.AbsolutePath,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://tournament.example/")), "allocation-token");
        var participantId = new ParticipantId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        await client.ReportStartedAsync();
        await client.ReportWinnerAsync(1, participantId);
        await client.ReportWinnerAsync(1, participantId); // An identical retry succeeds through the API.
        await client.ReportPlacementsAsync(2, [new BoutPlacement(participantId, 1, 10)]);
        await client.ReportNoShowsAsync(3, [participantId]);
        Assert.Equal(5, requests.Count);
        Assert.Equal(("POST", "/api/v1/game-server/match/started", null), requests[0]);
        Assert.Equal(("PUT", "/api/v1/game-server/match/bouts/1", "{\"winner\":\"33333333-3333-3333-3333-333333333333\"}"), requests[1]);
        Assert.Equal(requests[1], requests[2]);
        Assert.Equal(("PUT", "/api/v1/game-server/match/bouts/2", "{\"placements\":[{\"participantId\":\"33333333-3333-3333-3333-333333333333\",\"placement\":1,\"points\":10}]}"), requests[3]);
        Assert.Equal(("POST", "/api/v1/game-server/match/bouts/3/no-shows", "{\"participantIds\":[\"33333333-3333-3333-3333-333333333333\"]}"), requests[4]);
    }
}
