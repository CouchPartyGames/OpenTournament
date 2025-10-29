using ErrorOr;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Matches.Get;

public static class GetMatchHandler
{
    public static async Task<ErrorOr<GetMatchResponse>> HandleAsync(string id, 
        AppDbContext dbContext, 
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(id);
        if (matchId is null)
        {
            return Error.Validation();
        }

        var match = await dbContext
            .Matches
            .Include(m => m.Participant1)
            .Include(m => m.Participant2)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (match is null)
        {
            return Error.NotFound();
        }

        return new  GetMatchResponse(match);
    }
}