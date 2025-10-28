using Microsoft.AspNetCore.Http.Results;

namespace OpenTournament.Core.Features.Competitions;

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