namespace OpenTournament.Tests.Integration;


public class HealthCheckTest(TournamentApiFactory factory) : IClassFixture<TournamentApiFactory>
{
    private readonly HttpClient _httpClient = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_ShouldReturn200_WhenGetRequest()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
