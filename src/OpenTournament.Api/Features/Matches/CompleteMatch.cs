using MassTransit;
using Microsoft.AspNetCore.Authorization;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Features.Matches;

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
        
        var match = await dbContext
            .Matches
            .FirstOrDefaultAsync(x => x.Id == matchId, token);
        if (match is null)
        {
            return TypedResults.NotFound();
        }

        if (winnerId != match.Participant1Id && match.Participant2Id != winnerId)
        {
            return TypedResults.Conflict();
        }

        if (match.State == MatchState.Complete)
        {
            return TypedResults.Conflict();
        }


        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

            // Complete Match
            match.Complete(winnerId);
            var msg = new MatchCompleted {
                MatchId = matchId,
                TournamentId = match.TournamentId,
                WinnerId = match.WinnerId,
                CompletedLocalMatchId = match.LocalMatchId
            };

            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            // Publish Results
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:match-completed"));
            await endpoint.Send(msg, token);
        });

        return TypedResults.Ok();
    }
}