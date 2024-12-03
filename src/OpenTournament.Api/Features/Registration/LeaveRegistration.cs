using MassTransit;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Features.Registration;

public static class LeaveRegistration
{

    public static async Task<Results<NoContent, ValidationProblem, NotFound>> Endpoint(string id,
        IMediator mediator,
        HttpContext context,
        AppDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken token)
    {
        var participantClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
        }
        var participantId = new ParticipantId(participantClaim.Value);
        
        var registration = await dbContext
            .Registrations
            .FirstOrDefaultAsync(r => r.ParticipantId == participantId
                                        && r.TournamentId == tournamentId, token);
        if (registration is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Remove(registration);
        await dbContext.SaveChangesAsync(token);

        var msg = new PlayerLeft() {
            TournamentId = tournamentId, 
            ParticipantId = participantId
        };
        await publishEndpoint.Publish(msg, token);

        return TypedResults.NoContent();
    }
}