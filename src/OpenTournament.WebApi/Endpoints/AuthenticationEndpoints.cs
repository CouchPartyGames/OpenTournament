using OpenTournament.Core.Features.Authentication;

namespace OpenTournament.WebApi.Endpoints;

public static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/login", async () =>
            {
                
            })
            .WithTags("Authentication")
            .WithSummary("Login")
            .WithDescription("Login/Register a user")
            .AllowAnonymous();


        /*
        builder.MapPost("/register", Register.Endpoint)
            .WithTags("Authentication", "Register")
            .WithSummary("Register")
            .WithDescription("Register a user")
            .WithOpenApi()
            .AllowAnonymous();
            */
        
        return builder;
    }
}