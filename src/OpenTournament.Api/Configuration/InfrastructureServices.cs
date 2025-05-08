using Microsoft.AspNetCore.HttpLogging;
using OpenTournament.Api.Configuration.Infrastructure;

namespace OpenTournament.Api.Configuration;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgres(configuration);
        services.AddAuthentication(configuration);
        services.AddAuthorizationServices();
        services.AddRabbitMq(configuration);
        services.AddObservability(configuration);
        services.AddHttpLogging((options) =>
        {
            options.CombineLogs = true;
            options.LoggingFields = HttpLoggingFields.All;
        });
        services.AddHybridCacheServices(configuration);
        services.AddHealthChecks();
        
        return services;
    }
}