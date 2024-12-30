namespace OpenTournament.Api.Configuration.Infrastructure;

public static class GoogleAuthenticationServices
{
    public static IServiceCollection AddGoogleAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication()
            .AddGoogle(opts =>
            {
                opts.ClientId = configuration["Google:ClientId"];
                opts.ClientSecret = configuration["Google:ClientSecret"];
                opts.CallbackPath = "/signin-google";
            });
        return services;
    }
}