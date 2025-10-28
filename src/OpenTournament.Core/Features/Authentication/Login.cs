using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Api.Identity.Authentication;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Core.Features.Authentication;

public static class Login
{

    public static async Task<Results<NoContent, ForbidHttpResult, Conflict, NotFound>> Endpoint( 
        HttpContext httpContext,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var userId = httpContext.GetUserId();
        if (userId is null)
        {
            return TypedResults.Forbid();
        }

        ParticipantId participantId = new ParticipantId(userId);
        var participant = await dbContext
            .Participants
            .FirstOrDefaultAsync(p => p.Id == participantId, token);
        if (participant is not null)
        {
            return TypedResults.Conflict();
        } 
            
        var newParticipant = new Participant()
        {
            Id = participantId,
            Name = "hello",
            Rank = 1
        };
        await dbContext.AddAsync(newParticipant, token);
        await dbContext.SaveChangesAsync(token);
        
        return TypedResults.NoContent();
    }
}