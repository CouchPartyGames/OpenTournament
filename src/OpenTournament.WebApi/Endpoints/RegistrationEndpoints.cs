using OpenTournament.Api.Features.Registration;

namespace OpenTournament.WebApi.Endpoints;

public static class RegistrationEndpoints
{
    
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
}