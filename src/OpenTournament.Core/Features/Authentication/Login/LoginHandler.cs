using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using ErrorOr;

namespace OpenTournament.Core.Features.Authentication.Login;

public static class LoginHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(string userId,
        AppDbContext dbContext, 
        CancellationToken token)
    {
        if (userId == "")
        {
            return Error.Forbidden();
        }

        ParticipantId participantId = new ParticipantId(userId);
        var participant = await dbContext
            .Participants
            .FirstOrDefaultAsync(p => p.Id == participantId, token);
        if (participant is not null)
        {
            return Error.NotFound();
        } 
            
        var newParticipant = new Participant()
        {
            Id = participantId,
            Name = "hello",
            Rank = 1
        };
        await dbContext.AddAsync(newParticipant, token);
        await dbContext.SaveChangesAsync(token);

        return Result.Success;
    }
}