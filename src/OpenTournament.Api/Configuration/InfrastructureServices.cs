using Microsoft.AspNetCore.HttpLogging;
using OpenTournament.Api.Configuration.Infrastructure;

namespace OpenTournament.Api.Configuration;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddPostgres(configuration);
        
        // Authentication
        services.AddFirebaseAuthentication(configuration);
        
        // Authorization
        services.AddAuthorizationServices();
        
        // Messaging
        services.AddRabbitMq(configuration);
        
        // Observability (OpenTelemetry)
        services.AddObservability(configuration);
        
        // HTTP Logging
        services.AddHttpLogging((options) =>
        {
            options.CombineLogs = true;
            options.LoggingFields = HttpLoggingFields.All;
        });
        
        // Caching
        services.AddHybridCacheServices(configuration);
        
        // Health Check
        services.AddHealthChecks();
        
        return services;
    }
}