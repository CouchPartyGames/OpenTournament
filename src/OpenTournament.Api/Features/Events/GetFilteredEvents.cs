using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Features.Events;

public static class GetFilteredEvents
{
    
    public static async Task<Results<Ok<List<Event>>, NotFound>> Endpoint(
        AppDbContext dbContext,
        CancellationToken token)
    {
        var list = await dbContext.Events.ToListAsync(token);
        return TypedResults.Ok(list);
    }
}