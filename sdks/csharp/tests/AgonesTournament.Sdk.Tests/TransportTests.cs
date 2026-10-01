using System.Net;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

public class TransportTests
{
    [Fact]
    public async Task PreservesDeploymentPrefixAndSetsTokenPerRequestWithoutChangingHttpClient()
    {
        using var fakeHttp = new HttpHandler((request, _) =>
        {
            Assert.Equal("https://example.com/tournaments/api/v1/game-server/match/started", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer match-token", request.Headers.Authorization!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        using var http = new HttpClient(fakeHttp) { BaseAddress = new Uri("https://other.example/") };
        http.DefaultRequestHeaders.Authorization = new("Bearer", "other-token");
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com/tournaments/")), "match-token");
        await client.ReportStartedAsync();
        Assert.Equal("https://other.example/", http.BaseAddress.AbsoluteUri);
        Assert.Equal("other-token", http.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>Bad Gateway</html>")]
    public async Task NonProblemErrorStillCarriesHttpStatus(string body)
    {
        using var fakeHttp = new HttpHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(body)
        }));
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com")), "token");
        var error = await Assert.ThrowsAsync<ApiException>(() => client.ReportStartedAsync());
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.Equal("http-error", error.Code);
    }

    [Fact]
    public async Task CancellationReachesTheTransportAndRemainsCancellation()
    {
        using var fakeHttp = new HttpHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com")), "token");
        using var cancellation = new CancellationTokenSource();
        var pending = client.ReportStartedAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData("https://other.example/api")]
    [InlineData("//other.example/api")]
    [InlineData("../other-api")]
    public async Task RejectsPathsThatCouldSendTheTokenOutsideTheConfiguredService(string path)
    {
        using var fakeHttp = new HttpHandler((_, _) => throw new InvalidOperationException("Unexpected request"));
        using var http = new HttpClient(fakeHttp);
        var transport = new TournamentHttpClient(http, new Uri("https://example.com/tournaments/"));
        await Assert.ThrowsAsync<ArgumentException>(() => transport.SendAsync(HttpMethod.Post, path, "secret-token"));
    }
}
