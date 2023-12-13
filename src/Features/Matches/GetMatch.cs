using OpenTournament.Common;

namespace Features.Matches;

public static class GetMatch
{
    public sealed record GetMatchQuery(MatchId Id) : IRequest<OneOf<Match, OneOf.Types.NotFound>>;

    internal sealed class Handler : IRequestHandler<GetMatchQuery, OneOf<Match, OneOf.Types.NotFound>>
    {
        private AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<Match, OneOf.Types.NotFound>> Handle(GetMatchQuery request,
            CancellationToken token)
        {
            var match = await _dbContext
                .Matches
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (match is null)
            {
                return new OneOf.Types.NotFound();
            }
            return match;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("matches/{id}", Endpoint)
            .WithTags("Match")
            .WithDescription("Update a Match");

    public static async Task<Results<Ok<Match>, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        if (Guid.TryParse(id, out Guid guidOutput))
        {
            return TypedResults.NotFound();
        }
        
        var request = new GetMatchQuery(new MatchId(guidOutput));
        var result = await mediator.Send(request, token);
        return result.Match<Results<Ok<Match>, NotFound>>(
            match => TypedResults.Ok(match),
            _ => TypedResults.NotFound()
            );
    }
}