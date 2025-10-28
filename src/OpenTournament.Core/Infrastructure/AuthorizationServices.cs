using Microsoft.Extensions.DependencyInjection;

namespace OpenTournament.Core.Infrastructure;

public static class AuthorizationServices
{
    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyExtensions.ParticipantPolicyName, 
                AuthorizationPolicyExtensions.GetParticipantPolicy());
            
            options.AddPolicy(AuthorizationPolicyExtensions.ServerPolicyName, 
                AuthorizationPolicyExtensions.GetServerPolicy());

                // Set a Fallback Policy to apply to all non explicit endpoints
            options.FallbackPolicy = AuthorizationPolicyExtensions.GetParticipantPolicy();
            options.DefaultPolicy = options.FallbackPolicy;
        });
        //services.AddSingleton<IAuthorizationHandler, TournamentDeleteHandler>();
        //services.AddSingleton<IAuthorizationHandler, MatchCompleteHandler>();
        //services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
        
        return services;
    }
}