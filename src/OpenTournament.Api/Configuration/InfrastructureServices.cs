using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTournament.Api.Configuration.Infrastructure;
using OpenTournament.Api.Data;
using OpenTournament.Api.Identity;
using OpenTournament.Api.Identity.Authorization;
using OpenTournament.Api.Identity.Authorization.Handlers;
using OpenTournament.Api.Jobs;
using OpenTournament.Api.Observability;

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