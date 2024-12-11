using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class KeycloakAuthenticationServices
{
    public static IServiceCollection AddKeycloakAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(AuthenticationSchemes.Keycloak, opts =>
            {
                opts.RequireHttpsMetadata = false;
                opts.Audience = configuration["Authentication:Keycloak:Audience"];
                opts.MetadataAddress = configuration["Authentication:Keycloak:MetadataAddress"];
                opts.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Authentication:Keycloak:ValidateIssuer"],
                };
            });
        return services;
    }
}