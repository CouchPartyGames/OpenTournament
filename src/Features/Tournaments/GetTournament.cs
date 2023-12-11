using OpenTournament.Common;
using Microsoft.EntityFrameworkCore;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace Features.Tournaments;

public static class GetTournament
{
    public sealed record Query(Guid Id) : IRequest<OneOf<Tournament, OneOf.Types.NotFound>>;


    internal sealed class
        Handler : IRequestHandler<Query, OneOf<Tournament, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<Tournament, OneOf.Types.NotFound>> Handle(Query request,
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
        app.MapGet("tournaments/{id}", GetTournament.EndPoint);
	
	
    
    public static async Task<Results<Ok<Tournament>, NotFound>> EndPoint(Query request, 
        IMediator mediator, 
        CancellationToken token)
    {
        var result = await mediator.Send(request, token);
        return result.Match<Results<Ok<Tournament>, NotFound>>(
            tournament => TypedResults.Ok(tournament),
            _ => TypedResults.NotFound());
    }
}