using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Features.Matches;

public static class GetMatch
{
    public sealed record GetMatchQuery(MatchId Id) : IRequest<OneOf<Match, OneOf.Types.NotFound>>;

    public sealed record GetMatchResponse(Match Match);


    public static async Task<Results<Ok<Match>, ValidationProblem, NotFound>> Endpoint(string id,
        IMediator mediator,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(id);
        if (matchId is null)
        {
            return TypedResults.ValidationProblem(ValidationErrors.MatchIdFailure);
        }

        var match = await dbContext
            .Matches
            .Include(m => m.Participant1)
            .Include(m => m.Participant2)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (match is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(match);
    }
}