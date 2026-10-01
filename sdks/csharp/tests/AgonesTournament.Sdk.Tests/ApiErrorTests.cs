using System.Net;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

public class ApiErrorTests
{
    [Theory]
    [InlineData("match-token-invalid", 401, typeof(MatchTokenInvalidException))]
    [InlineData("bout-conflict", 409, typeof(BoutConflictException))]
    [InlineData("bout-already-forfeited", 409, typeof(BoutAlreadyForfeitedException))]
    [InlineData("bout-out-of-order", 409, typeof(BoutOutOfOrderException))]
    [InlineData("validation-failed", 422, typeof(ValidationException))]
    [InlineData("bad-request", 400, typeof(ValidationException))]
    [InlineData("future-problem", 400, typeof(ApiException))]
    public async Task PreservesProblemCodeDetailAndFieldsForReadsAndWrites(string code, int status, Type exceptionType)
    {
        using var fakeHttp = new HttpHandler((_, _) => Task.FromResult(HttpHandler.Json($$"""
            {"type":"https://example.com/problems/{{code}}","title":"Problem","status":{{status}},
             "code":"{{code}}","detail":"Specific reason","errors":[{"location":"body.winner","message":"Not a Participant","value":"unknown"}]}
            """, (HttpStatusCode)status)));
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com")), "token");
        foreach (var call in new Func<Task>[] { async () => await client.MatchAsync(), () => client.ReportStartedAsync() })
        {
            var error = await Assert.ThrowsAsync(exceptionType, call);
            var apiError = Assert.IsAssignableFrom<ApiException>(error);
            Assert.Equal(code, apiError.Code);
            Assert.Equal("Specific reason", apiError.Detail);
            Assert.Equal((HttpStatusCode)status, apiError.StatusCode);
            Assert.Equal("body.winner", Assert.Single(apiError.Problem.Errors!).Location);
            Assert.Equal("unknown", Assert.Single(apiError.Problem.Errors!).Value!.Value.GetString());
        }
    }

    [Fact]
    public async Task DifferentResultForARecordedBoutSurfacesAsTypedConflict()
    {
        var calls = 0;
        using var fakeHttp = new HttpHandler((_, _) => Task.FromResult(++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : HttpHandler.Json("""{"title":"Conflict","status":409,"code":"bout-conflict","detail":"a different result is already recorded for this bout"}""", HttpStatusCode.Conflict)));
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com")), "token");
        await client.ReportWinnerAsync(1, new ParticipantId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        var error = await Assert.ThrowsAsync<BoutConflictException>(() =>
            client.ReportWinnerAsync(1, new ParticipantId(Guid.Parse("22222222-2222-2222-2222-222222222222"))));
        Assert.Equal("bout-conflict", error.Code);
    }
}
