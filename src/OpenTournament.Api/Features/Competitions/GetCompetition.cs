using OpenTournament.Api.Data;

namespace OpenTournament.Api.Features.Competitions;

public static class GetCompetition
{
    
    public static async Task<Results<Ok, NotFound>> Endpoint(
        string id,
        AppDbContext dbContext,
        CancellationToken token)
    {
        return TypedResults.Ok();
    }
}