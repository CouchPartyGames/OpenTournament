using Features.Tournaments;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Common;

namespace OpenTournament.Tests.Integration;

public class CreateTournamentTest : IClassFixture<TournamentApiFactory>
{
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;

    public CreateTournamentTest(TournamentApiFactory factory, ITestOutputHelper output)
    {
        _httpClient = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Endpoint_ShouldCreateTournament_WhenDataIsValid()
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