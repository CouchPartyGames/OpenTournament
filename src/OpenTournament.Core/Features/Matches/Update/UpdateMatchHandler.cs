using OpenTournament.Core.Infrastructure.Persistence;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Features.Matches.Update;

public static class UpdateMatchHandler
{
    public static async ValueTask<ErrorOr<Updated>> HandleAsync(
        UpdateMatchCommand command,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(command.Id);
        if (matchId is null)
        {
            return Error.Validation();
        }
        
        var match = await dbContext
            .Matches
            .FirstOrDefaultAsync(m => m.Id == matchId, token);
        if (match is null)
        {
            return Error.NotFound();
        }
            
        /* Is user allowed to update match
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, document, new EditRequirement());
        if (!authorizationResult.Succeeded)
        {
            return new OneOf.Types.Error<>();
        }*/
            
        //match.Status
        var result = await dbContext.SaveChangesAsync(token);
        if (result == 0)
        {
            return Error.Failure();
        }
        
        return Result.Updated;
    }
}