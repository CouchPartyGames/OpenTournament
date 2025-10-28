using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Events;

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