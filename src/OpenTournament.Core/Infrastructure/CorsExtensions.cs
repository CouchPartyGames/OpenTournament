using Microsoft.AspNetCore.Cors.Infrastructure;

namespace OpenTournament.Core.Infrastructure;

public static class CorsExtensions
{
    public const string ProdCorsPolicyName = "prod";

    public static CorsPolicy GetProdCorsPolicy() =>
        new CorsPolicyBuilder()
            .WithOrigins("https://api.opentournament.online")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(30))
            .Build();

    
    public const string DevCorsPolicyName = "dev";
    
    public static CorsPolicy GetDevCorsPolicy() =>
        new CorsPolicyBuilder()
            .AllowAnyOrigin()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .Build();
}