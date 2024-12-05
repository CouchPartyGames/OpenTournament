using Microsoft.AspNetCore.Authorization;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Identity.Authorization.Requirements;

namespace OpenTournament.Api.Identity.Authorization.Handlers;

public sealed class HasEventAdminAccessHandler : AuthorizationHandler<MatchCompleteRequirement, EventAdmin>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MatchCompleteRequirement requirement,
        EventAdmin resource)
    {
        return Task.CompletedTask;
    }
}
