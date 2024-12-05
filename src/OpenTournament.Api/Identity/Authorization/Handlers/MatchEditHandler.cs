using Microsoft.AspNetCore.Authorization;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.Identity.Authorization.Requirements;

namespace OpenTournament.Api.Identity.Authorization.Handlers;

public sealed class MatchEditHandler : AuthorizationHandler<MatchEditRequirement, Match>
{
   protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
      MatchEditRequirement requirement,
      Match match)
   {
      if (context.User.HasClaim(c => c.Subject.Name == match.Id.Value.ToString() ))
      {
         context.Succeed(requirement);
      }
      return Task.CompletedTask;
   }
}