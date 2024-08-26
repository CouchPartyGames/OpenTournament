using OpenTournament.Data.Models;
using OpenTournament.Features.Tournaments;

namespace OpenTournament.Tests.Integration;

public class GetTournamentTest(TournamentApiFactory factory, ITestOutputHelper outputHelper) 
    : IClassFixture<TournamentApiFactory>
{
    private readonly HttpClient _httpClient = factory.CreateClient();

    [Fact]
    public async Task Endpoint_ShouldGetTournament_WhenDataIsValid()
    {
        /*
        // Arrange
        var id = TournamentId.NewTournamentId();
        //StringContent jsonContext = new GetTournament.GetTournamentQuery(id);
        
        // Act
        var response = await _httpClient.PostAsync("/tournaments", jsonContext);

        // A
        */
    }
}