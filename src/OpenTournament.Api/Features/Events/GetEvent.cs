using OpenTournament.Api.Data;

namespace OpenTournament.Api.Features.Events;

public static class GetEvent
{

    public static async Task<Results<Ok, NotFound>> Endpoint(
        string id,
        AppDbContext dbContext,
        CancellationToken token)
    {
        return TypedResults.Ok();
    }
}