using OpenTournament.Data.Models;
using OpenTournament.Features;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace Features.Tournaments;

public static class GetTournament
{
    public sealed record GetTournamentQuery(TournamentId Id) : IRequest<OneOf<Tournament, OneOf.Types.NotFound, ValidationProblem>>;

    public sealed record GetTournamentResponse(Tournament Tournament);
    

    internal sealed class
        Handler : IRequestHandler<GetTournamentQuery, OneOf<Tournament, OneOf.Types.NotFound, ValidationProblem>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<Tournament, OneOf.Types.NotFound, ValidationProblem>> Handle(GetTournamentQuery request,
            CancellationToken cancellationToken)
        {
            var tournament = await _dbContext
                .Tournaments
                .Include(m => m.Matches)
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (tournament is null)
            {
                return new OneOf.Types.NotFound();
            }

            return tournament;
        }
    }


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("tournaments/{id}/", Endpoint)
            .WithTags("Tournament")
            .WithSummary("Get Tournament")
            .WithDescription("Return an existing tournament.")
            .WithOpenApi()
            .AllowAnonymous();
	
	
    
    public static async Task<Results<Ok<Tournament>, NotFound, ValidationProblem>> Endpoint(string id, 
        IMediator mediator, 
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
        }


        var request = new GetTournamentQuery(tournamentId);
        var result = await mediator.Send(request, token);
        return result.Match<Results<Ok<Tournament>, NotFound, ValidationProblem>>(
            tournament => TypedResults.Ok(tournament),
            _ => TypedResults.NotFound(),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>()));
    }
}