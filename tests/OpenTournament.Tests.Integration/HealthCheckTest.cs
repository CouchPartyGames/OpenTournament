namespace OpenTournament.Tests.Integration;

public class HealthCheckTest : IClassFixture<TournamentApiFactory>
{
    private readonly HttpClient _httpClient;

    public HealthCheckTest(TournamentApiFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200_WhenGetRequest()
    {
        // 
        var response = await _httpClient.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}