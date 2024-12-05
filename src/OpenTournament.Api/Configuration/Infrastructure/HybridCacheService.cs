using Microsoft.Extensions.Caching.Hybrid;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class HybridCacheService
{
    public static IServiceCollection AddHybridCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HybridCacheOptions>(configuration.GetSection(HybridCacheOptions.SectionName));
        
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