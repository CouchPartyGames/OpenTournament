using Features.Tournaments;

namespace OpenTournament.Tests.Integration;

public class CreateTournamentTest
{
    private readonly WebApplicationFactory<IApiMarker> _factory = new();

    private readonly HttpClient _httpClient;

    public CreateTournamentTest()
    {
        _httpClient = _factory.CreateClient();
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