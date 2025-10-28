using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Features.Matches.Update;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.WebApi.Endpoints;

public static class MatchEndpoints
{
    
    public static IEndpointRouteBuilder MapMatchEndpoints(this RouteGroupBuilder builder)
    {

        builder.MapPut("/{id}/complete", async (string id,
                    CompleteMatch.CompleteMatchCommand command,
                    ISendEndpointProvider sendEndpointProvider,
                    AppDbContext dbContext,
                    IAuthenticationService authenticationService,
                    CancellationToken token) =>
                await CompleteMatch.Endpoint(id, command, sendEndpointProvider, dbContext, token))
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match");
        
        builder.MapGet("/{id}/", GetMatch.Endpoint)
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