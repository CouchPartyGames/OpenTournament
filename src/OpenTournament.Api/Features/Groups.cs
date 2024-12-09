using MassTransit;
using Microsoft.AspNetCore.Authentication;
using OpenTournament.Api.Data;
using OpenTournament.Api.Features.Authentication;
using OpenTournament.Api.Features.Matches;
using OpenTournament.Api.Features.Registration;
using OpenTournament.Api.Features.Templates;
using OpenTournament.Api.Features.Tournaments;

namespace OpenTournament.Api.Features;

public static class Groups
{

    public static RouteGroupBuilder MapAuthenticationEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/login", Login.Endpoint)
            .WithTags("Authentication", "Login")
            .WithSummary("Login")
            .WithDescription("Login/Register a user")
            .WithOpenApi()
            .RequireAuthorization();


        /*
        builder.MapPost("/register", Register.Endpoint)
            .WithTags("Authentication", "Register")
            .WithSummary("Register")
            .WithDescription("Register a user")
            .WithOpenApi();
            */
        
        return builder;
    }

    public static RouteGroupBuilder MapMatchesEndpoints(this RouteGroupBuilder builder)
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
                IMediator mediator,
                AppDbContext dbContext,
                CancellationToken token) => await UpdateTournament.Endpoint(id, request, mediator, dbContext, token))
            .WithTags("Tournament")
            .WithSummary("Update Tournament")
            .WithDescription("Update Tournament settings")
            .WithOpenApi()
            .RequireAuthorization();
        
        return builder;
    }

    public static RouteGroupBuilder MapTemplatesEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Create Template")
            .WithDescription("Create a Tournament Template")
            .WithOpenApi();
        
        
        builder.MapDelete("/{id}", DeleteTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Delete Template")
            .WithDescription("Delete an available Template")
            .WithOpenApi();
        
        builder.MapPut("/templates/{id}", UpdateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Update Template")
            .WithDescription("Update a Tournament Template")
            .WithOpenApi();
        
        return builder;
    }
}