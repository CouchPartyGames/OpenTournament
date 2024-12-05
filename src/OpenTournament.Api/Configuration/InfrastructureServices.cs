using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTournament.Api.Data;
using OpenTournament.Api.Identity;
using OpenTournament.Api.Identity.Authorization;
using OpenTournament.Api.Identity.Authorization.Handlers;
using OpenTournament.Api.Jobs;
using OpenTournament.Api.Observability;
using OpenTournament.Api.Options;

namespace OpenTournament.Api.Configuration;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        DatabaseOptions dbOptions = new();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(dbOptions);
        
        services.AddDbContext<AppDbContext>(opts =>
        {
            var connectionString = dbOptions.ConnectionString;
            opts.UseNpgsql(connectionString, pgOpts =>
                {
                    //pgOpts.EnableRetryOnFailure(4);
                    pgOpts.CommandTimeout(15);
                    //pgOpts.ExecutionStrategy();
                })
                .EnableSensitiveDataLogging()
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Singleton);
        
        // Authentication
        services.Configure<FirebaseAuthenticationOptions>(
        configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
        
        // Authentication
        services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            var firebaseAuth = configuration
                .GetSection(FirebaseAuthenticationOptions.SectionName)
                .Get<FirebaseAuthenticationOptions>();

            options.Authority = firebaseAuth.Authority;
            options.TokenValidationParameters = new()
            {
                ValidIssuer = firebaseAuth.Issuer,
                ValidAudience = firebaseAuth.Audience,
                
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };
        });
        
        // Authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityData.ParticipantPolicyName, policyBuilder =>
            {
                policyBuilder.RequireAuthenticatedUser();
            });
            
            options.AddPolicy(IdentityData.ServerPolicyName, policyBuilder =>
            {
                policyBuilder.RequireClaim(IdentityData.ServerClaimName, "server");
            });
        });
        //services.AddSingleton<IAuthorizationHandler, TournamentDeleteHandler>();
        //services.AddSingleton<IAuthorizationHandler, MatchCompleteHandler>();
        //services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
        
        // Observability (OpenTelemetry)
        services.AddObservability(configuration);
        
        // Messaging
        services.AddMassTransit(opts => {
            opts.SetKebabCaseEndpointNameFormatter();

            opts.AddConsumer<TournamentStartedConsumer>();
            opts.AddConsumer<MatchCompletedConsumer>();

            //opts.UsingInMemory();
            opts.UsingRabbitMq((context, cfg) => {
                cfg.Host("localhost", h => {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });
        
        // HTTP Logging
        services.AddHttpLogging((options) =>
        {
            options.CombineLogs = true;
            options.LoggingFields = HttpLoggingFields.All;
        });
        
        #pragma warning disable
        services.AddHybridCache(opts =>
        {
            opts.MaximumKeyLength = 256;
            opts.MaximumPayloadBytes = 1024;
            opts.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(4)
            };
        });
        #pragma warning restore
        
        return services;
    }
}