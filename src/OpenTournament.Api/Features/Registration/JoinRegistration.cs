using MassTransit;
using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class JoinRegistration
{

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("registrations/{id}/join", Endpoint)
            .WithTags("Registration")
            .WithSummary("Join Tournament")
            .WithDescription("Allow a user to register for a specific tournament")
            .WithOpenApi()
            .RequireAuthorization();

    
    public static async Task<Results<NoContent, BadRequest, NotFound, ProblemHttpResult>> Endpoint(string id, 
        HttpContext context,
        IMediator mediator, 
        AppDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken token)
    {

        var participantClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        
        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
        if (tournament is null)
        {
            return TypedResults.BadRequest();
        }

            // Rules
        var engine = new RuleEngine();
        engine.Add(new TournamentInRegistrationState(tournament.Status));
        if (!engine.Evaluate())
        {
            return TypedResults.NotFound();
            //return new RuleFailure(engine.Errors);
        }

        var participantId = new ParticipantId(participantClaim.Value);
        dbContext.Add(Registration.Create(tournamentId, participantId));
        var result = await dbContext.SaveChangesAsync(token);
        if (result < 1)
        {
        }

        var msg = new PlayerJoined {
           TournamentId = tournamentId,
           ParticipantId = participantId 
        };
        await publishEndpoint.Publish(msg, token);

        return TypedResults.NoContent();
    }
}