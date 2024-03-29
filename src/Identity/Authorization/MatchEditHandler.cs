﻿using Microsoft.AspNetCore.Authorization;
using OpenTournament.Data.Models;

namespace OpenTournament.Identity.Authorization;

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