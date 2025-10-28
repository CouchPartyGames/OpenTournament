using Microsoft.AspNetCore.Authorization;
using OneOf.Types;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Api.Features.Tournaments;

public static class DeleteTournament
{
    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, 
        IMediator mediator, 
        AppDbContext dbContext, 
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        var tournament = dbContext
            .Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        dbContext.Remove(tournament);
        await dbContext.SaveChangesAsync(token);

        return TypedResults.NoContent();
    }
}