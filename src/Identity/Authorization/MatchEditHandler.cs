using Microsoft.AspNetCore.Authorization;
using OpenTournament.Models;

namespace OpenTournament.Identity.Authorization;

public sealed class MatchEditHandler : AuthorizationHandler<MatchEditRequirement, Match>
{
   protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
      MatchEditRequirement requirement,
      Match match)
   {
      if (context.User.HasClaim(c => c.Subject.Name == "placeholder" ))
      {
         context.Succeed(requirement);
      }
      return Task.CompletedTask;
   }
}