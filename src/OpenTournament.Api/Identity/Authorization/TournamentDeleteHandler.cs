using Microsoft.AspNetCore.Authorization;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Identity.Authorization;

public sealed class TournamentDeleteHandler : AuthorizationHandler<DeleteTournamentRequirement, Tournament>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
        DeleteTournamentRequirement requirement,
        Tournament resource)
    {
        if (context.User.Identity.Name == resource.Id.Value.ToString())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}