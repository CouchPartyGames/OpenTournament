using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenTournament.Core.Infrastructure;

public static class HybridCacheService
{
    public static IServiceCollection AddHybridCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HybridCacheOptions>(configuration.GetSection(HybridCacheOptions.SectionName));

        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = "";
            opts.InstanceName = "";
        });
        
        #pragma warning disable
        services.AddHybridCache(opts =>
        {
            opts.ReportTagMetrics = true;
            opts.MaximumKeyLength = 64;
            opts.MaximumPayloadBytes = 1024 * 1024; // 1024 * 1024 = 1MB
            opts.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });
            //.AddSerializer<MessagePack>();
            //.AddSerializer<GoogleProtobufSerializer>();
        #pragma warning restore
        
        return services;
    }
}