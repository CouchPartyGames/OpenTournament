using Microsoft.AspNetCore.Cors.Infrastructure;

namespace OpenTournament.WebApi.Middleware.Cors;

public static class CorsPolicies
{
    
    public const string Production = "Production";
    public const string Development = "Development";

    public static CorsPolicy DevelopmentPolicy()
    {
        return new CorsPolicyBuilder()
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(1))
            .Build();
    }
    
    public static CorsPolicy ProductionPolicy()
    {
        return new CorsPolicyBuilder()
            .WithOrigins("https://api.opentournament.online")
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(30))
            .Build();
    }
}