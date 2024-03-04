using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class ListRegistration
{
    public sealed record ListRegistrationResponse();
    
    public sealed record ListRegistrationQuery(TournamentId TournamentId) : IRequest<ListRegistrationResponse>;


    /*
    internal sealed class
        Handler : IRequestHandler<ListRegistrationQuery, ListRegistrationResponse>
    {
        private readonly AppDbContext _dbContext;
        
        public async ValueTask<> Handle(ListRegistrationQuery query, CancellationToken token) 
        {
            var participants = await _dbContext.Registrations
                .Where(x => x.TournamentId == query.TournamentId)
                .Select(x => x.Participant)
                .ToListAsync();
                
            return participants;
        }
    }
    */

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("registrations/{id}/", Endpoint)
            .WithTags("Registration")
            .WithSummary("List Participants")
            .WithDescription("List all participants in a tournament")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<Ok<ListRegistrationResponse>, NotFound, ProblemHttpResult>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        
        if (!Guid.TryParse(id, out Guid tournyGuid))
        {
            return TypedResults.Problem(new ProblemDetails());
            return TypedResults.NotFound();
        }
        
        var query = new ListRegistrationQuery(new TournamentId(tournyGuid));
        /*
        var result = await mediator.Send(query, token);
        results.Match {
            _ => TypedResults.Ok(),
            validate => validate.ExecuteAsync(httpContext),
            problem => problem.ExecuteAsync(httpContext)
        }
        */

        return TypedResults.Ok(new ListRegistrationResponse());
    }
}