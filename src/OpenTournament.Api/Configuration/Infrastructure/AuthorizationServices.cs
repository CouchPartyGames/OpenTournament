using OpenTournament.Api.Identity;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class AuthorizationServices
{
    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
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
        
        return services;
    }
    
}