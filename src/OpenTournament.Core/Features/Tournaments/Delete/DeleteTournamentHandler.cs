using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Delete;

using ErrorOr;

public static class DeleteTournamentHandler
{
    public static async Task<ErrorOr<Deleted>> HandleAsync(string id, AppDbContext dbContext, CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
            //return TypedResults.NotFound();
        }
        var tournament = dbContext
            .Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        dbContext.Remove(tournament);
        await dbContext.SaveChangesAsync(token);

        return Result.Deleted;
    }
    
}