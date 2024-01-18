namespace OpenTournament.Tests.Integration;

public class HealthCheckTest : IClassFixture<TournamentApiFactory>
{
    private readonly HttpClient _httpClient;

    public HealthCheckTest(TournamentApiFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact(Skip="Not ready")]
    public async Task HealthCheck_ShouldReturn200_WhenGetRequest()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}