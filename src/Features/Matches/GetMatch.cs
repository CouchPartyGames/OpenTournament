using OpenTournament.Data.Models;

namespace Features.Matches;

public static class GetMatch
{
    public sealed record GetMatchQuery(MatchId Id) : IRequest<OneOf<Match, OneOf.Types.NotFound>>;

    public sealed record GetMatchResponse(Match Match);

    internal sealed class Handler : IRequestHandler<GetMatchQuery, OneOf<Match, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<Match, OneOf.Types.NotFound>> Handle(GetMatchQuery request,
            CancellationToken token)
        {
            var match = await _dbContext
                .Matches
                .Include(m => m.Participant1)
                .Include(m => m.Participant2)
                .FirstOrDefaultAsync(m => m.Id == request.Id, token);

            if (match is null)
            {
                return new OneOf.Types.NotFound();
            }
            return match;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("matches/{id}/", Endpoint)
            .WithTags("Match")
            .WithSummary("Get Matches")
            .WithDescription("Get Matches")
            .WithOpenApi()
            .AllowAnonymous();

    public static async Task<Results<Ok<Match>, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(id);
        if (matchId is null)
        {
            return TypedResults.NotFound();
        }

        var request = new GetMatchQuery(matchId);
        var result = await mediator.Send(request, token);
        return result.Match<Results<Ok<Match>, NotFound>>(
            match => TypedResults.Ok(match),
            _ => TypedResults.NotFound()
            );
    }
}