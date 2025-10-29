using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Registration.List;

public static class ListRegistration
{
    public sealed record ListRegistrationResponse(List<Participant> Registrations);


    public static async Task<Results<Ok<ListRegistrationResponse>, NotFound, ValidationProblem>> Endpoint(string id,
        IMediator mediator,
        HttpContext httpContext,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        
        var participants = await dbContext
            .Registrations
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToListAsync(cancellationToken: token);
        
        return TypedResults.Ok(new ListRegistrationResponse(participants));
    }
}