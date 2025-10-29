using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Features.Matches.Complete;
using OpenTournament.Core.Features.Matches.Get;
using OpenTournament.Core.Features.Matches.Update;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.WebApi.Endpoints;

public static class MatchEndpoints
{
    
    public static RouteGroupBuilder MapMatchEndpoints(this RouteGroupBuilder builder)
    {

        builder.MapPut("/{id}/complete", async Task<Results<NoContent, BadRequest>> (string id,
            CompleteMatchCommand command,
            //ISendEndpointProvider sendEndpointProvider,
            AppDbContext dbContext,
            //IAuthenticationService authenticationService,
            CancellationToken token) =>
        {
            var result = await CompleteMatchHandler.HandleAsync(command, dbContext, token);
            return result switch
            {
                { IsError: false } => TypedResults.NoContent(),
                _ => TypedResults.BadRequest()
            };
        })
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match");
        
        
        builder.MapGet("/{id}/", async Task<Results<Ok<GetMatchResponse>, BadRequest>> (string id, AppDbContext dbContext, CancellationToken token) =>
            {
                var result = await GetMatchHandler.HandleAsync(id, dbContext, token);
                return result switch
                {
                    { IsError: false } => TypedResults.Ok(result.Value),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Match")
            .WithSummary("Get Matches")
            .WithDescription("Get Matches")
            .AllowAnonymous();

        
        builder.MapPut("/{id}", async Task<Results<NoContent, BadRequest>> (string id,
                AppDbContext dbContext,
                CancellationToken token) =>
            {
                var command = new UpdateMatchCommand(id);
                var result = await UpdateMatchHandler.HandleAsync(command, dbContext, token);
                return result switch
                {
                    { IsError: false} => TypedResults.NoContent(),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Match")
            .WithSummary("Update Match")
            .WithDescription("Update Individual Match");
        
        return builder;
    }
}