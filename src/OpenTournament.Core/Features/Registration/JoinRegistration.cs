using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.Events;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Api.Features.Registration;

public static class JoinRegistration
{
    public static async Task<Results<NoContent, ValidationProblem, NotFound, Conflict>> Endpoint(string id, 
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
            return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
        }
        
        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
        if (tournament is null)
        {
            return TypedResults.NotFound();
        }

            // Rules
            /*
        var engine = new RuleEngine();
        engine.Add(new TournamentInRegistrationState(tournament.Status));
        if (!engine.Evaluate())
        {
            return TypedResults.Conflict();
        }*/

        var participantId = new ParticipantId(participantClaim.Value);
        dbContext.Add(Core.Domain.Entities.Registration.Create(tournamentId, participantId));
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