using MassTransit;
using Microsoft.AspNetCore.Authentication;
using OpenTournament.Api.Data;
using OpenTournament.Api.Features.Matches;

namespace OpenTournament.WebApi.Endpoints;

public static class MatchEndpoints
{
    
    public static RouteGroupBuilder MapMatchEndpoints(this RouteGroupBuilder builder)
    {
        
        builder.MapPut("/{id}/complete", async (string id,
                CompleteMatch.CompleteMatchCommand command,
                ISendEndpointProvider sendEndpointProvider,
                AppDbContext dbContext,
                IAuthenticationService authenticationService,
                CancellationToken token) => await CompleteMatch.Endpoint(id, command, sendEndpointProvider, dbContext, token))
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match")
            .WithOpenApi();
        
        builder.MapGet("/{id}/", GetMatch.Endpoint)
            .WithTags("Match")
            .WithSummary("Get Matches")
            .WithDescription("Get Matches")
            .WithOpenApi()
            .AllowAnonymous();
        
        builder.MapPut("/{id}", (string id,
                UpdateMatch.UpdateMatchCommand cmd,
                IMediator mediator,
                CancellationToken token) =>
            {
                return UpdateMatch.Endpoint(id, cmd, mediator, token);
            })
            .WithTags("Match")
            .WithSummary("Update Match")
            .WithDescription("Update Individual Match")
            .WithOpenApi();
        
        return builder;
    }
}