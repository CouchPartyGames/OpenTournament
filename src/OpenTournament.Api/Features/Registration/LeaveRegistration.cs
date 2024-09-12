using MassTransit;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class LeaveRegistration
{

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("registrations/{id}/leave", Endpoint)
            .WithTags("Registration")
            .WithSummary("Leave Tournament")
            .WithDescription("Allow a user to deregister from a specific Tournament")
            .WithOpenApi()
            .RequireAuthorization();


    public static async Task<Results<NoContent, BadRequest, NotFound>> Endpoint(string id,
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
            return TypedResults.BadRequest();
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
        var result = await dbContext.SaveChangesAsync(token);

        var msg = new PlayerLeft() {
            TournamentId = tournamentId, 
            ParticipantId = participantId
        };
        await publishEndpoint.Publish(msg, token);

        return TypedResults.NoContent();
    }
}