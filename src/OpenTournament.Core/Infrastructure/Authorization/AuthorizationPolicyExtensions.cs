using Microsoft.AspNetCore.Authorization;

namespace OpenTournament.Core.Infrastructure;

public static class AuthorizationPolicyExtensions
{
    
    public const string ParticipantPolicyName = "participant";
    
    public static AuthorizationPolicy GetParticipantPolicy() 
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    
    
    public const string ServerPolicyName = "server";
    private const string ServerClaimName = "server";
    
    public static AuthorizationPolicy GetServerPolicy() =>  
        new AuthorizationPolicyBuilder()
            .RequireClaim(ServerClaimName, "server")
            .Build();
}