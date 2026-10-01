using AgonesTournament.Sdk.Core;
using TournamentGameClient = AgonesTournament.Sdk.GameClient.GameClient;

namespace AgonesTournament.Sdk.Tests;

public class GameClientTests
{
    [Fact]
    public async Task ListsTournamentsAnonymouslyWithEscapedGameAndStatusFilters()
    {
        using var fakeHttp = new HttpHandler((request, _) =>
        {
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("https://tournament.example/prefix/api/v1/tournaments?gameId=marbles%20%26%20more&status=registration-open&limit=20&offset=5", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(HttpHandler.Json("{\"tournaments\":null}"));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example/prefix")));
        var page = await client.ListTournamentsAsync("marbles & more", TournamentStatus.RegistrationOpen, 20, 5);
        Assert.Null(page.Tournaments);
    }
    [Fact]
    public async Task RegistersOwnAndLinkedPlayerIdentitiesWithFreshTokens()
    {
        var calls = 0;
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/participants", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer token-" + ++calls, request.Headers.Authorization!.ToString());
            Assert.Equal(calls == 1 ? "{}" : "{\"identity\":{\"kind\":\"steam\",\"value\":\"external-id\"}}",
                await request.Content!.ReadAsStringAsync(ct));
            return HttpHandler.Json("""
                {"id":"33333333-3333-3333-3333-333333333333","identity":{"kind":"steam","value":"external-id"},
                 "status":"checked-in","registeredAt":"2026-10-01T12:00:00Z","checkedInAt":"2026-10-01T12:00:00Z"}
                """);
        });
        using var http = new HttpClient(fakeHttp);
        var issued = 0;
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token-" + ++issued));
        var id = new TournamentId(Guid.NewGuid());
        await client.RegisterAsync(id);
        var participant = await client.RegisterAsync(id, new PlayerIdentity { Kind = "steam", Value = "external-id" });
        Assert.Equal(ParticipantStatus.CheckedIn, participant.Status);
        Assert.Equal("external-id", participant.Identity.Value);
        Assert.NotNull(participant.CheckedInAt);
    }

    [Fact]
    public async Task SignInRequiredOperationsFailBeforeSendingWithoutATokenProvider()
    {
        using var fakeHttp = new HttpHandler((_, _) => throw new Exception("No request should be sent"));
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")));
        var id = new TournamentId(Guid.NewGuid());
        var participantId = new ParticipantId(Guid.NewGuid());
        foreach (var call in new Func<Task>[] {
            () => client.RegisterAsync(id), () => client.UnregisterAsync(id, participantId),
            () => client.CheckInAsync(id, participantId), () => client.MyRegistrationsAsync(id) })
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(call);
            Assert.Contains("Keycloak access token provider", error.Message);
        }
    }
    [Theory]
    [InlineData("registration-closed", typeof(RegistrationClosedException))]
    [InlineData("tournament-full", typeof(TournamentFullException))]
    [InlineData("already-registered", typeof(AlreadyRegisteredException))]
    [InlineData("identity-kind-not-accepted", typeof(PlayerIdentityKindNotAcceptedException))]
    [InlineData("identity-not-owned", typeof(PlayerIdentityNotOwnedException))]
    [InlineData("check-in-closed", typeof(CheckInClosedException))]
    public async Task RegistrationAndCheckInFailuresCarryTypedProblemErrors(string code, Type exceptionType)
    {
        using var fakeHttp = new HttpHandler((_, _) => Task.FromResult(HttpHandler.Json(
            "{\"title\":\"Conflict\",\"status\":409,\"code\":\"" + code + "\",\"detail\":\"explanation\"}",
            System.Net.HttpStatusCode.Conflict)));
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token"));
        var id = new TournamentId(Guid.NewGuid());
        var error = await Assert.ThrowsAsync(exceptionType, () => code == "check-in-closed"
            ? client.CheckInAsync(id, new ParticipantId(Guid.NewGuid())) : client.RegisterAsync(id));
        var problem = Assert.IsAssignableFrom<ApiException>(error);
        Assert.Equal(code, problem.Code);
        Assert.Equal("explanation", problem.Detail);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, problem.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MatchEndpointIsAbsentUnlessIncludedByTheApi(bool disclosed)
    {
        using var fakeHttp = new HttpHandler((request, _) =>
        {
            Assert.Equal(disclosed ? "Bearer token" : null, request.Headers.Authorization?.ToString());
            return Task.FromResult(HttpHandler.Json("""
                {"id":"11111111-1111-1111-1111-111111111111","tournamentId":"22222222-2222-2222-2222-222222222222",
                 "groupId":"44444444-4444-4444-4444-444444444444","key":"R1-M1","round":1,"status":"in-progress",
                 "participants":["33333333-3333-3333-3333-333333333333"],"bouts":null,"aborts":0,"serverAllocated":true
                """ + (disclosed ? ",\"serverAddress\":\"192.0.2.1\",\"serverPort\":7777}" : "}")));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            disclosed ? _ => Task.FromResult("token") : null);
        var match = await client.MatchAsync(new MatchId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        Assert.True(match.ServerAllocated);
        Assert.Equal(disclosed ? "192.0.2.1" : null, match.ServerAddress);
        Assert.Equal(disclosed ? 7777 : (int?)null, match.ServerPort);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EmptyProviderTokensFailClearly(string? token)
    {
        using var fakeHttp = new HttpHandler((_, _) => throw new Exception("No request should be sent"));
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult(token!));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.RegisterAsync(new TournamentId(Guid.NewGuid())));
        Assert.Contains("empty token", error.Message);
    }

    [Fact]
    public async Task CancellationReachesTheTokenProviderAndHttpRequest()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var fakeHttp = new HttpHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(HttpHandler.Json("{\"tournaments\":null}"));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")), ct =>
        {
            Assert.Equal(cancelled.Token, ct);
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("token");
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ListTournamentsAsync(cancellationToken: cancelled.Token));
        var anonymous = new TournamentGameClient(new TournamentHttpClient(http, new Uri("https://tournament.example")));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => anonymous.ListTournamentsAsync(cancellationToken: cancelled.Token));
    }
}
