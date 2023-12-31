using OpenTournament.Common;
using OpenTournament.Common.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace Features.Tournaments;

public static class GetTournament
{
    public sealed record GetTournamentQuery(TournamentId Id) : IRequest<OneOf<Tournament, OneOf.Types.NotFound>>;


    internal sealed class
        Handler : IRequestHandler<GetTournamentQuery, OneOf<Tournament, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<Tournament, OneOf.Types.NotFound>> Handle(GetTournamentQuery request,
            CancellationToken cancellationToken)
        {
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (tournament is null)
            {
                return new OneOf.Types.NotFound();
            }

            return tournament;
        }
    }

    
    public static void MapEndpoint(this IEndpointRouteBuilder app) => 
        app.MapGet("tournaments/{id}", Endpoint).WithTags("Tournament");
	
	
    
    public static async Task<Results<Ok<Tournament>, NotFound>> Endpoint(string id, 
        IMediator mediator, 
        CancellationToken token)
    {
        if (Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }
        
        var request = new GetTournamentQuery(new TournamentId(guid));
        var result = await mediator.Send(request, token);
        return result.Match<Results<Ok<Tournament>, NotFound>>(
            tournament => TypedResults.Ok(tournament),
            _ => TypedResults.NotFound());
    }
}