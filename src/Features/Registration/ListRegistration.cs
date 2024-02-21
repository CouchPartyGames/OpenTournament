using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class ListRegistration
{
    /*
    public sealed record ListRegistrationQuery(TournamentId TournamentId) : IRequest<>;
    
    
    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("registrations/{id}/", Endpoint)
            .WithTags("Registration")
            .WithSummary("List Participants")
            .WithDescription("List all participants in a tournament")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<bool, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid tournyGuid))
        {
            return TypedResults.Problem(statusCode: 422);
        }
        
        var query = new ListRegistrationQuery(new TournamentId(tournyGuid));
        var result = await mediator.Send(query, token);

        return true;
    }
    */
}