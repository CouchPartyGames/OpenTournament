using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Events;

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