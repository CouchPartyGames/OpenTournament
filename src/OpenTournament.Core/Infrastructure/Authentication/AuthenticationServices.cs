using Microsoft.Extensions.DependencyInjection;

namespace OpenTournament.Core.Infrastructure.Authentication;

public static class AuthenticationServices
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddAuthentication().AddCookie();
        return services;
    }
}