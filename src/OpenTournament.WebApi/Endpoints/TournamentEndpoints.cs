using OpenTournament.Api.Data;
using OpenTournament.Api.Features.Tournaments;

namespace OpenTournament.WebApi.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateTournament.EndPoint)
            .WithTags("Tournament")
            .WithSummary("Create Tournament")
            .WithDescription("Create a new tournament")
            .WithOpenApi()
            .RequireAuthorization();
        
        builder.MapGet("/{id}/", GetTournament.Endpoint)
            .WithTags("Tournament")
            .WithSummary("Get Tournament")
            .WithDescription("Return an existing tournament.")
            .WithOpenApi()
            .AllowAnonymous();
        
        builder.MapDelete("/{id}", DeleteTournament.Endpoint)
            .WithTags("Tournament")
            .WithSummary("Delete Tournament")
            .WithDescription("Delete an existing tournament")
            .WithOpenApi();
        
        builder.MapPut("/{id}/start", StartTournament.Endpoint)
            .WithTags("Tournament")
            .WithSummary("Start Tournament")
            .WithDescription("Mark the tournament as ready to begin")
            .WithOpenApi()
            .RequireAuthorization();
            
        builder.MapPut("/{id}", async (string id,
                UpdateTournament.UpdateTournamentCommand request,
                AppDbContext dbContext,
                CancellationToken token) => await UpdateTournament.Endpoint(id, request, dbContext, token))
            .WithTags("Tournament")
            .WithSummary("Update Tournament")
            .WithDescription("Update Tournament settings")
            .WithOpenApi()
            .RequireAuthorization();
        
        return builder;
    }
    
}