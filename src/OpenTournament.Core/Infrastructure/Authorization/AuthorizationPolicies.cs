namespace OpenTournament.Core.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authorization;


public static class AuthorizationPolicies
{
    public const string Production = "Production";

    public static AuthorizationPolicy GetDefault() => new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
}