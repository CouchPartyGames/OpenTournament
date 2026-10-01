using System.Net;
using System.Text.Json;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.Organizer;

namespace AgonesTournament.Sdk.Tests;

public class OrganizerClientTests
{
    private static readonly TournamentId TournamentId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly ParticipantId ParticipantId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    internal const string TournamentJson = """
        {"id":"22222222-2222-2222-2222-222222222222","gameId":"marbles","name":"Weekly Tournament",
         "organizer":"client:backend","status":"draft","startsAt":"2026-10-02T12:00:00Z",
         "registrationOpensAt":"2026-10-02T11:30:00Z","capacity":16,"minimumParticipants":2,
         "checkIn":{"enabled":false},"stages":null,"registered":0,"checkedIn":0,
         "createdAt":"2026-10-01T12:00:00Z","updatedAt":"2026-10-01T12:00:00Z"}
        """;
    internal const string ParticipantJson = """
        {"id":"33333333-3333-3333-3333-333333333333","identity":{"kind":"steam","value":"external-id"},
         "status":"registered","registeredAt":"2026-10-02T11:30:00Z"}
        """;

    [Fact]
    public async Task CreatesTournamentWithSettingsAndReturnsItsAssignedId()
    {
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://tournament.example/prefix/api/v1/tournaments", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer backend-token", request.Headers.Authorization!.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal("marbles", body.RootElement.GetProperty("gameId").GetString());
            Assert.Equal(3, body.RootElement.GetProperty("stages")[0].GetProperty("bestOf").GetInt32());
            Assert.False(body.RootElement.TryGetProperty("checkIn", out _));
            Assert.False(body.RootElement.GetProperty("stages")[0].TryGetProperty("bouts", out _));
            return HttpHandler.Json(TournamentJson, HttpStatusCode.Created);
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example/prefix")),
            _ => Task.FromResult("backend-token"));
        var tournament = await client.CreateTournamentAsync(NewTournament());
        Assert.Equal(TournamentId, tournament.Id);
        Assert.Equal(TournamentStatus.Draft, tournament.Status);
    }

    [Fact]
    public async Task EditsDraftSettingsWithoutChangingGameOrSendingUnsetCheckInWindow()
    {
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.EndsWith("/tournaments/" + TournamentId, request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.False(body.RootElement.TryGetProperty("gameId", out _));
            Assert.Equal("Renamed", body.RootElement.GetProperty("name").GetString());
            Assert.Equal("{\"enabled\":false}", body.RootElement.GetProperty("checkIn").GetRawText());
            return HttpHandler.Json(TournamentJson);
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token"));
        // A creation request is also settings, but editing must never send its Game.
        TournamentSettings settings = NewTournament() with { Name = "Renamed", CheckIn = new CheckInSettings { Enabled = false } };
        Assert.Equal(TournamentId, (await client.EditTournamentAsync(TournamentId, settings)).Id);
    }

    [Fact]
    public async Task ReadsAndCancelsTournamentsWithFreshTokensAndEscapedFilters()
    {
        var issued = 0;
        var calls = 0;
        using var fakeHttp = new HttpHandler((request, _) =>
        {
            Assert.Equal("Bearer token-" + ++calls, request.Headers.Authorization!.ToString());
            Assert.Null(request.Content);
            if (request.RequestUri!.Query.Length > 0)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("?gameId=marbles%20%26%20more&status=draft&limit=20&offset=5", request.RequestUri.Query);
                return Task.FromResult(HttpHandler.Json("{\"tournaments\":null}"));
            }
            Assert.Equal(calls == 3 ? HttpMethod.Post : HttpMethod.Get, request.Method);
            Assert.EndsWith("/tournaments/" + TournamentId + (calls == 3 ? "/cancel" : ""), request.RequestUri.AbsolutePath);
            return Task.FromResult(HttpHandler.Json(calls == 3 ? TournamentJson.Replace("draft", "cancelled") : TournamentJson));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token-" + ++issued));
        Assert.Null((await client.ListTournamentsAsync("marbles & more", TournamentStatus.Draft, 20, 5)).Tournaments);
        Assert.Equal(TournamentId, (await client.TournamentAsync(TournamentId)).Id);
        Assert.Equal(TournamentStatus.Cancelled, (await client.CancelTournamentAsync(TournamentId)).Status);
    }

    [Fact]
    public async Task TrustedBackendRegistersChecksInListsAndDisqualifiesParticipant()
    {
        var calls = 0;
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            Assert.Equal("Bearer backend-token", request.Headers.Authorization!.ToString());
            var path = "/api/v1/tournaments/" + TournamentId + "/participants";
            switch (++calls)
            {
                case 1:
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal(path, request.RequestUri!.AbsolutePath);
                    Assert.Equal("{\"identity\":{\"kind\":\"steam\",\"value\":\"external-id\"}}", await request.Content!.ReadAsStringAsync(ct));
                    return HttpHandler.Json(ParticipantJson, HttpStatusCode.Created);
                case 2:
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal(path + "/" + ParticipantId + "/check-in", request.RequestUri!.AbsolutePath);
                    Assert.Null(request.Content);
                    return HttpHandler.Json(ParticipantJson.Replace("registered\"", "checked-in\""));
                case 3:
                    Assert.Equal(HttpMethod.Get, request.Method);
                    Assert.Equal(path, request.RequestUri!.AbsolutePath);
                    return HttpHandler.Json("{\"participants\":null}");
                default:
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal(path + "/" + ParticipantId + "/disqualify", request.RequestUri!.AbsolutePath);
                    Assert.Null(request.Content);
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("backend-token"));
        Assert.Equal(ParticipantId, (await client.RegisterAsync(TournamentId, new PlayerIdentity { Kind = "steam", Value = "external-id" })).Id);
        Assert.Equal(ParticipantStatus.CheckedIn, (await client.CheckInAsync(TournamentId, ParticipantId)).Status);
        Assert.Null((await client.ListParticipantsAsync(TournamentId)).Participants);
        await client.DisqualifyAsync(TournamentId, ParticipantId);
    }

    [Theory]
    [InlineData("settings-frozen", 409, typeof(SettingsFrozenException))]
    [InlineData("declared-in-git", 409, typeof(DeclaredInGitException))]
    [InlineData("not-trusted-for-game", 403, typeof(NotTrustedForGameException))]
    [InlineData("validation-failed", 422, typeof(ValidationException))]
    public async Task ManagementErrorsPreserveTypedProblemsAndValidationFields(string code, int status, Type exceptionType)
    {
        using var fakeHttp = new HttpHandler((_, _) => Task.FromResult(HttpHandler.Json($$"""
            {"title":"Problem","status":{{status}},"code":"{{code}}","detail":"explanation",
             "errors":[{"location":"body.stages[0].bestOf","message":"must be 1 or 3","value":5}]}
            """, (HttpStatusCode)status)));
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token"));
        var error = await Assert.ThrowsAsync(exceptionType, () => client.EditTournamentAsync(TournamentId, NewTournament()));
        var problem = Assert.IsAssignableFrom<ApiException>(error);
        Assert.Equal(code, problem.Code);
        Assert.Equal("explanation", problem.Detail);
        Assert.Equal((HttpStatusCode)status, problem.StatusCode);
        Assert.Equal(5, Assert.Single(problem.Problem.Errors!).Value!.Value.GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EmptyAccessTokensFailBeforeHttpForEveryOperation(string? token)
    {
        using var fakeHttp = new HttpHandler((_, _) => throw new Exception("No request should be sent"));
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult(token!));
        foreach (var call in new Func<Task>[]
        {
            () => client.CreateTournamentAsync(NewTournament()), () => client.EditTournamentAsync(TournamentId, NewTournament()),
            () => client.TournamentAsync(TournamentId), () => client.ListTournamentsAsync(), () => client.CancelTournamentAsync(TournamentId),
            () => client.ListParticipantsAsync(TournamentId),
            () => client.RegisterAsync(TournamentId, new PlayerIdentity { Kind = "steam", Value = "external-id" }),
            () => client.CheckInAsync(TournamentId, ParticipantId), () => client.DisqualifyAsync(TournamentId, ParticipantId)
        })
            Assert.Contains("empty token", (await Assert.ThrowsAsync<InvalidOperationException>(call)).Message);
    }

    [Fact]
    public async Task CancellationReachesTheTokenProviderAndHttpTransport()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var fakeHttp = new HttpHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(HttpHandler.Json(TournamentJson));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")), ct =>
        {
            Assert.Equal(cancelled.Token, ct);
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("token");
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CancelTournamentAsync(TournamentId, cancelled.Token));
        var uncancelledProvider = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("token"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uncancelledProvider.CheckInAsync(TournamentId, ParticipantId, cancelled.Token));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(201, 0)]
    [InlineData(50, -1)]
    public async Task InvalidPaginationFailsBeforeHttp(int limit, int offset)
    {
        using var fakeHttp = new HttpHandler((_, _) => throw new Exception("No request should be sent"));
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => throw new Exception("No token should be requested"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ListTournamentsAsync(limit: limit, offset: offset));
    }

    internal static NewTournament NewTournament() => new()
    {
        GameId = "marbles", Name = "Weekly Tournament", Capacity = 16, MinimumParticipants = 2,
        StartsAt = DateTimeOffset.Parse("2026-10-02T12:00:00Z"),
        RegistrationOpensAt = DateTimeOffset.Parse("2026-10-02T11:30:00Z"),
        Stages = [new StageSettings { Format = MatchFormat.SingleElimination, BestOf = 3, ResultDeadlineSeconds = 600 }]
    };
}
