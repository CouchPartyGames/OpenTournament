using Microsoft.AspNetCore.Authorization;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Identity.Authorization.Requirements;

namespace OpenTournament.Api.Identity.Authorization.Handlers;

public sealed class TournamentDeleteHandler : AuthorizationHandler<TournamentDeleteRequirement, Tournament>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
        TournamentDeleteRequirement requirement,
        Tournament resource)
    {
        if (context.User.Identity.Name == resource.Id.Value.ToString())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}