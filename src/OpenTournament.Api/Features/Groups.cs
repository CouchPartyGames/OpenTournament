using Features.Matches;
using Features.Tournaments;
using MassTransit;
using OpenTournament.Features.Matches;
using OpenTournament.Features.Tournaments;
using OpenTournament.Features.Tournaments.Create;

namespace OpenTournament.Features;

public static class Groups
{

    public static RouteGroupBuilder MapAuthentication(this RouteGroupBuilder builder)
    {
        /*
        builder.MapPost("/auth/login", Endpoint)
            .WithTags("Auth")
            .WithSummary("Login")
            .WithDescription("Login/Register a user")
            .WithOpenApi()
            .RequireAuthorization();
            */
        
        return builder;
    }

    public static RouteGroupBuilder MapMatchesEndpoints(this RouteGroupBuilder builder)
    {
        
        builder.MapPut("/{id}/complete", (string id,
                CompleteMatch.CompleteMatchCommand command,
                ISendEndpointProvider sendEndpointProvider,
                AppDbContext dbContext,
                CancellationToken token) =>
            {
                return CompleteMatch.Endpoint(id, command, sendEndpointProvider, dbContext, token);
            })
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
    
    public static RouteGroupBuilder MapRegistrationEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPut("/{id}/join", JoinRegistration.Endpoint)
            .WithTags("Registration")
            .WithSummary("Join Tournament")
            .WithDescription("Allow a user to register for a specific tournament")
            .WithOpenApi()
            .RequireAuthorization();
        
        builder.MapDelete("/{id}/leave", LeaveRegistration.Endpoint)
            .WithTags("Registration")
            .WithSummary("Leave Tournament")
            .WithDescription("Allow a user to deregister from a specific Tournament")
            .WithOpenApi()
            .RequireAuthorization();
        
        builder.MapGet("/{id}/", ListRegistration.Endpoint)
            .WithTags("Registration")
            .WithSummary("List Participants")
            .WithDescription("List all participants in a tournament")
            .WithOpenApi()
            .RequireAuthorization();
        
        return builder;
    }
    
    public static RouteGroupBuilder MapTournamentsEndpoints(this RouteGroupBuilder builder)
    {
		builder.MapPost("", CreateTournamentEndpoint.EndPoint)
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
                IMediator mediator,
                CancellationToken token) => await UpdateTournament.Endpoint(id, request, mediator, token))
            .WithTags("Tournament")
            .WithSummary("Update Tournament")
            .WithDescription("Update Tournament settings")
            .WithOpenApi()
            .RequireAuthorization();
        
        return builder;
    }
}