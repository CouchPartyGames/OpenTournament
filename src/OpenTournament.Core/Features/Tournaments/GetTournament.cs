using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Api.Features.Tournaments;

public static class GetTournament
{
    public sealed record GetTournamentResponse(Tournament Tournament);
    
    
    public static async Task<Results<Ok<Tournament>, NotFound, ValidationProblem>> Endpoint(string id, 
        IMediator mediator, 
        AppDbContext dbContext,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
        }

        var tournament = await dbContext
            .Tournaments
            .Include(m => m.Matches)
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);

        return TypedResults.Ok(tournament);
    }
}