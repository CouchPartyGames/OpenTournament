namespace OpenTournament.Tests.Integration;

public class CreateTournamentTest : TournamentApiFactory
{
    private readonly HttpClient _httpClient;


    public CreateTournamentTest(WebApplicationFactory<IApiMarker> factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task Endpoint_ShouldReturn201_WhenSendingValidTournament()
    {
        StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Name = "Test",
                StartTime = DateTime.Now
            }),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _httpClient.PostAsync("/tournaments", jsonContent);

        // Arrange
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}