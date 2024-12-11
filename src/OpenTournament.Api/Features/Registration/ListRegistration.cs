using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Features.Registration;

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