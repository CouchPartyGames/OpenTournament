using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Matches;

public static class CompleteMatch
{
    public sealed record CompleteMatchCommand(string MatchId, string WinnerId) : IRequest<OneOf<Ok, NotFound>>;



    public static async Task<Results<Ok, NotFound, Conflict, ForbidHttpResult, ValidationProblem>> Endpoint(string id,
        CompleteMatchCommand command,
        ISendEndpointProvider sendEndpointProvider,
        AppDbContext dbContext,
        //IAuthorizationService authorizationService,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(command.MatchId);
        var winnerId = new ParticipantId(command.WinnerId);

        
        // Authorize Dedicated Hosts and Tournament Moderators
        
        /*
        var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, tournamentAdmins, new MatchCompleteRequirement());
        if (!authorizationResult.Succeeded)
        {
            return TypedResults.Forbid(); 
        }*/
            
        if (matchId is null)
        {
            return TypedResults.ValidationProblem(ValidationErrors.MatchIdFailure);
        }

        /*
        SELECT * FROM TournamentMatches WHERE TournamentMatches.Matches @> [{"MatchId":"matchId"}]
        var query = "[{\"MatchId\": \"{matchId}\"}]";
        var match = await dbContext
            .TournamentMatches
            .Where(x => x.Matches.Any(m => m.MatchId == matchId))
            .ToListAsync(token);
            //.Where(x => EF.Functions.Contains(x, query))
            //.ToListAsync(token);
            */


        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

            // Complete Match
            //match.Matches[0].MatchResults = null;
            //match.Matches[0].CompletedOnUtc = DateTime.UtcNow;
            
            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            // Publish Results
            //var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:match-completed"));
            //await endpoint.Send(msg, token);
        });

        return TypedResults.Ok();
    }
}