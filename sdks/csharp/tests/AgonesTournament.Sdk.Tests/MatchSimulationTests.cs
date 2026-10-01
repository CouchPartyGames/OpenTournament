using System.Net;
using AgonesTournament.ReferenceGameServer;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.Tests;

public class MatchSimulationTests
{
    private static readonly ParticipantId A = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly ParticipantId B = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private sealed class Recorder : IMatchReports
    {
        public List<string> Calls { get; } = [];
        public List<(int Bout, ParticipantId Winner)> Winners { get; } = [];
        public Queue<Exception> Failures { get; } = new();

        public Task ReportStartedAsync(CancellationToken cancellationToken)
        {
            Calls.Add("started");
            return Task.CompletedTask;
        }

        public Task ReportWinnerAsync(int bout, ParticipantId winner, CancellationToken cancellationToken)
        {
            Calls.Add($"bout {bout}");
            if (Failures.TryDequeue(out var failure)) throw failure;
            Winners.Add((bout, winner));
            return Task.CompletedTask;
        }
    }

    private static Match MatchOf(int bestOf, params Bout[] completed) => new()
    {
        MatchId = new(Guid.NewGuid()), TournamentId = new(Guid.NewGuid()), GameId = "arena",
        Status = MatchStatus.InProgress, Format = MatchFormat.SingleElimination, BestOf = bestOf,
        Participants =
        [
            new() { ParticipantId = A, IdentityKind = "keycloak", IdentityValue = "a", Forfeited = false },
            new() { ParticipantId = B, IdentityKind = "keycloak", IdentityValue = "b", Forfeited = false }
        ],
        CompletedBouts = completed.Length == 0 ? null : completed
    };

    private static MatchSimulation Simulation(Recorder reports, int seed, List<TimeSpan>? delays = null)
        => new(reports, new Random(seed), TimeSpan.FromSeconds(3), (span, _) =>
        {
            delays?.Add(span);
            return Task.CompletedTask;
        });

    private static ParticipantId[] ExpectedWinners(int seed, int count)
    {
        var random = new Random(seed);
        return [.. Enumerable.Range(0, count).Select(_ => random.Next(2) == 0 ? A : B)];
    }

    [Fact]
    public async Task BestOfOneReportsStartedThenOneBoutWithTheSeededRandomWinner()
    {
        var reports = new Recorder();
        await Simulation(reports, 7).PlayAsync(MatchOf(1), _ => { }, default);

        Assert.Equal(["started", "bout 1"], reports.Calls);
        Assert.Equal((1, ExpectedWinners(7, 1)[0]), Assert.Single(reports.Winners));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task BestOfThreeStopsAfterAParticipantsSecondWinAndReportsBoutsInOrder(int seed)
    {
        var reports = new Recorder();
        var delays = new List<TimeSpan>();
        await Simulation(reports, seed, delays).PlayAsync(MatchOf(3), _ => { }, default);

        Assert.Equal("started", reports.Calls[0]);
        Assert.Equal(Enumerable.Range(1, reports.Winners.Count), reports.Winners.Select(w => w.Bout));
        Assert.InRange(reports.Winners.Count, 2, 3);
        var tally = reports.Winners.GroupBy(w => w.Winner).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(2, tally.Values.Max());
        Assert.Equal(ExpectedWinners(seed, reports.Winners.Count), reports.Winners.Select(w => w.Winner));
        Assert.All(delays, d => Assert.Equal(TimeSpan.FromSeconds(3), d));
        Assert.Equal(reports.Winners.Count, delays.Count);
    }

    [Fact]
    public async Task BothParticipantsWinSomeBoutsAcrossSeeds()
    {
        var winners = new HashSet<ParticipantId>();
        for (var seed = 0; seed < 10; seed++)
        {
            var reports = new Recorder();
            await Simulation(reports, seed).PlayAsync(MatchOf(1), _ => { }, default);
            winners.Add(reports.Winners.Single().Winner);
        }
        Assert.Equal([A, B], winners.OrderBy(w => w.Value));
    }

    [Fact]
    public async Task ResumesAfterCompletedBoutsWithoutReplayingThem()
    {
        var reports = new Recorder();
        var completed = new Bout { Number = 1, Results = [new() { ParticipantId = A, Won = true }, new() { ParticipantId = B }] };
        await Simulation(reports, 3).PlayAsync(MatchOf(3, completed), _ => { }, default);

        Assert.Equal(2, reports.Winners[0].Bout);
        Assert.InRange(reports.Winners.Count, 1, 2);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task RetriesATransientFailureOnABoutReportWithTheSameWinner(HttpStatusCode status)
    {
        var reports = new Recorder();
        reports.Failures.Enqueue(Api(status, "unavailable"));
        reports.Failures.Enqueue(new HttpRequestException("connection reset"));
        await Simulation(reports, 7).PlayAsync(MatchOf(1), _ => { }, default);

        Assert.Equal(["started", "bout 1", "bout 1", "bout 1"], reports.Calls);
        Assert.Equal(ExpectedWinners(7, 1)[0], Assert.Single(reports.Winners).Winner);
    }

    [Fact]
    public async Task GivesUpAfterTheMaximumAttempts()
    {
        var reports = new Recorder();
        for (var i = 0; i < 5; i++) reports.Failures.Enqueue(new HttpRequestException("down"));
        await Assert.ThrowsAsync<HttpRequestException>(
            () => Simulation(reports, 7).PlayAsync(MatchOf(1), _ => { }, default));
        Assert.Equal(6, reports.Calls.Count); // started + 5 attempts
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "match-token-invalid")]
    [InlineData(HttpStatusCode.Conflict, "bout-conflict")]
    public async Task DoesNotRetryAPermanentRejection(HttpStatusCode status, string code)
    {
        var reports = new Recorder();
        reports.Failures.Enqueue(Api(status, code));
        await Assert.ThrowsAsync<ApiException>(
            () => Simulation(reports, 7).PlayAsync(MatchOf(1), _ => { }, default));
        Assert.Equal(["started", "bout 1"], reports.Calls);
    }

    [Fact]
    public async Task RejectsMatchesItCannotSimulate()
    {
        var match = MatchOf(1) with { Format = MatchFormat.FreeForAll, BestOf = null, Bouts = 3 };
        await Assert.ThrowsAsync<UnsupportedMatchException>(
            () => Simulation(new Recorder(), 1).PlayAsync(match, _ => { }, default));
    }

    private static ApiException Api(HttpStatusCode status, string code)
        => new(status, new ProblemDetails { Title = code, Status = (long)status, Code = code });
}
