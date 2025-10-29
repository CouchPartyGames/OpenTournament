using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Features.Tournaments.Create;
using OpenTournament.Core.Features.Tournaments.Delete;
using OpenTournament.Core.Features.Tournaments.Get;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.WebApi.Endpoints;

public static class TournamentEndpoints
{
    public static IEndpointRouteBuilder MapTournamentEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", async Task<Results<Created, BadRequest>> (CreateTournamentCommand command, 
                AppDbContext dbContext, 
                CancellationToken cancellationToken) =>
        {
            var result = await CreateTournamentHandler.HandleAsync(command, dbContext, cancellationToken);
            return result switch
            {
                { IsError: false } => TypedResults.Created(),
                _ => TypedResults.BadRequest()
            };
        })
            .WithTags("Tournament")
            .WithSummary("Create Tournament")
            .WithDescription("Create a new tournament")
            .RequireAuthorization();
        
        
        builder.MapGet("/{id}/", async Task<Results<Ok<GetTournamentResponse>, BadRequest>> (string id, AppDbContext dbContext, CancellationToken token) =>
            {
                var result = await GetTournamentHandler.HandleAsync(id, dbContext, token);
                return result switch
                {
                    { IsError: false } => TypedResults.Ok(result.Value),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Tournament")
            .WithSummary("Get Tournament")
            .WithDescription("Return an existing tournament.")
            .AllowAnonymous();

        builder.MapDelete("/{id}", async Task<Results<NoContent, BadRequest>> (string id, 
                AppDbContext dbContext, 
                CancellationToken token) =>
            {
                var result = await DeleteTournamentHandler.HandleAsync(id, dbContext, token);
                return result switch
                {
                    { IsError: false } => TypedResults.NoContent(),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Tournament")
            .WithSummary("Delete Tournament")
            .WithDescription("Delete an existing tournament");
        
        builder.MapPut("/{id}/start", StartTournament.Endpoint)
            .WithTags("Tournament")
            .WithSummary("Start Tournament")
            .WithDescription("Mark the tournament as ready to begin")
            .RequireAuthorization();
            
        builder.MapPut("/{id}", async (string id,
                UpdateTournament.UpdateTournamentCommand request,
                AppDbContext dbContext,
                CancellationToken token) => await UpdateTournament.Endpoint(id, request, dbContext, token))
            .WithTags("Tournament")
            .WithSummary("Update Tournament")
            .WithDescription("Update Tournament settings")
            .RequireAuthorization();
        
        return builder;
    }
    
}