using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class ListRegistration
{
    public sealed record ListRegistrationQuery(string Id) : IRequest<OneOf<List<Participant>, ProblemDetails>>;
    
    public sealed record ListRegistrationResponse(List<Participant> Registrations);


    internal sealed class
        Handler : IRequestHandler<ListRegistrationQuery, OneOf<List<Participant>, ProblemDetails>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext appDbContext)
        {
            _dbContext = appDbContext;
        }
        
        public async ValueTask<OneOf<List<Participant>, ProblemDetails>> Handle(ListRegistrationQuery query, CancellationToken token)
        {
            var tournamentId = TournamentId.TryParse(query.Id);
            if (tournamentId is null)
            {
                return new ProblemDetails();
            }
            
            var participants = await _dbContext
                .Registrations
                .Where(x => x.TournamentId == tournamentId)
                .Select(x => x.Participant)
                .ToListAsync();
                
            return participants;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("registrations/{id}/", Endpoint)
            .WithTags("Registration")
            .WithSummary("List Participants")
            .WithDescription("List all participants in a tournament")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<Ok<ListRegistrationResponse>, ValidationProblem>> Endpoint(string id,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken token)
    {
        var result = await mediator.Send(new ListRegistrationQuery(id), token);
        return result.Match<Results<Ok<ListRegistrationResponse>, ValidationProblem>> (
            participants =>
            {
                return TypedResults.Ok(new ListRegistrationResponse(participants));
            },
            _ =>
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>());
            });
    }
}