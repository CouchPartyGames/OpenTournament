using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Api.Features.Competitions;
using OpenTournament.Core.Features.Competitions.Create;
using OpenTournament.Core.Features.Competitions.Get;
using OpenTournament.Core.Infrastructure.Persistence;
using Created = Microsoft.AspNetCore.Http.HttpResults.Created;

namespace OpenTournament.WebApi.Endpoints;

public static class CompetitionEndpoints
{
    
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", async Task<Results<Created, BadRequest>> (CreateCompetitionCommand command, 
                AppDbContext dbContext, 
                CancellationToken ct) =>
            {
                var result = await CreateCompetitionHandler.HandleAsync(command, dbContext, ct);
                return result switch
                {
                    { IsError: false } => TypedResults.Created(),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Competition")
            .WithSummary("Create Competition")
            .WithDescription("Create a Competition within an Event");
        
        builder.MapGet("/{id}", async Task<Results<Ok, BadRequest>> (GetCompetitionQuery query,
                AppDbContext dbContext,
                CancellationToken ct) =>
            {
                var result = await GetCompetitionHandler.HandleAsync(query, dbContext, ct);
                return result switch
                {
                    { IsError: false } => TypedResults.Ok(),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Competition")
            .WithSummary("Get Competition")
            .WithDescription("Get a Competition within an Event");

        /*
        builder.MapDelete("/{id}", DeleteCompetition.Endpoint)
            .WithTags("Competition")
            .WithSummary("Get Competition")
            .WithDescription("Get a Competition within an Event")
            .WithOpenApi();
            */
        
        return builder;
    }
}