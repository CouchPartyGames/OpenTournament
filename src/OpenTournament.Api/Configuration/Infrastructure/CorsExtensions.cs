using Microsoft.AspNetCore.Cors.Infrastructure;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class CorsExtensions
{
    public const string ProdCorsPolicyName = "prod";

    public static CorsPolicy GetProdCorsPolicy() =>
        new CorsPolicyBuilder()
            .WithOrigins("https://api.opentournament.online")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .AllowCredentials()
            .Build();

    
    public const string DevCorsPolicyName = "dev";
    
    public static CorsPolicy GetDevCorsPolicy() =>
        new CorsPolicyBuilder()
            .AllowAnyOrigin()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .Build();
}