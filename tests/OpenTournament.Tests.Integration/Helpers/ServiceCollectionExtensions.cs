using Microsoft.Extensions.DependencyInjection;

namespace OpenTournament.Tests.Integration.Helpers;

public static class ServiceCollectionExtensions
{
    public static void Remove<T>(this IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}