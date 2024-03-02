using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class ListRegistration
{
    public sealed record ListRegistrationQuery(TournamentId TournamentId) : IRequest<Ok>;


    /*
    internal sealed class
        Handler : IRequestHandler<ListRegistrationQuery, bool>
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

    public static async Task<Results<Ok, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        
        if (!Guid.TryParse(id, out Guid tournyGuid))
        {
            return TypedResults.NotFound();
            //return TypedResults.Problem(statusCode: 422);
        }
        
        var query = new ListRegistrationQuery(new TournamentId(tournyGuid));
        /*
        var result = await mediator.Send(query, token);
        */

        return TypedResults.Ok();
    }
}