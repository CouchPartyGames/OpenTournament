using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Registration.Leave;

public static class LeaveRegistrationHandler
{

    public static async Task<ErrorOr<Deleted>> HandleAsync(string id, 
        HttpContext httpContext,
        AppDbContext dbContext, 
        CancellationToken token)
    {
        var participantClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }
        var participantId = new ParticipantId(participantClaim.Value);
        
        var registration = await dbContext
            .Registrations
            .FirstOrDefaultAsync(r => r.ParticipantId == participantId
                                      && r.TournamentId == tournamentId, token);
        if (registration is null)
        {
            return Error.NotFound();
        }

        dbContext.Remove(registration);
        await dbContext.SaveChangesAsync(token);

        /*
        var msg = new PlayerLeft() {
            TournamentId = tournamentId, 
            ParticipantId = participantId
        };
        await publishEndpoint.Publish(msg, token);
        */

        return Result.Deleted;
    }
}