using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Features.Registration.Join;
using OpenTournament.Core.Features.Registration.Leave;
using OpenTournament.Core.Features.Registration.List;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.WebApi.Endpoints;

public static class RegistrationEndpoints
{
    
    public static RouteGroupBuilder MapRegistrationEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPut("/{id}/join", async Task<Results<NoContent, BadRequest>> (string id, 
                HttpContext httpContext, 
                AppDbContext dbContext, 
                CancellationToken ct) =>
            {
                var result = await JoinRegistrationHandler.HandleAsync(id, httpContext, dbContext, ct);
                return result switch
                {
                    { IsError: false } => TypedResults.NoContent(),
                    _  => TypedResults.BadRequest()
                };
            })
            .WithTags("Registration")
            .WithSummary("Join Tournament")
            .WithDescription("Allow a user to register for a specific tournament")
            .RequireAuthorization();
        
        
        builder.MapDelete("/{id}/leave", async Task<Results<NoContent, BadRequest>> (string id, 
                HttpContext httpContext,
                AppDbContext dbContext, 
                CancellationToken ct) =>
            {
                var result = await LeaveRegistrationHandler.HandleAsync(id, httpContext, dbContext, ct);
                return result switch
                {
                    { IsError: false } => TypedResults.NoContent(),
                    _  => TypedResults.BadRequest()
                };
            })
            .WithTags("Registration")
            .WithSummary("Leave Tournament")
            .WithDescription("Allow a user to deregister from a specific Tournament")
            .RequireAuthorization();
        
        
        builder.MapGet("/{id}/", async Task<Results<Ok<ListRegistrationResponse>, BadRequest>> (string id, AppDbContext dbContext, CancellationToken ct) =>
            {
                var result = await ListRegistrationHandler.HandleAsync(id, dbContext, ct);
                return result switch
                {
                    { IsError: false } => TypedResults.Ok(result.Value),
                    _  => TypedResults.BadRequest()
                };
            })
            .WithTags("Registration")
            .WithSummary("List Participants")
            .WithDescription("List all participants in a tournament")
            .RequireAuthorization();
        
        return builder;
    }
}