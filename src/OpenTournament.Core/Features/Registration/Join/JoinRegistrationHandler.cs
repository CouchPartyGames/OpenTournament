using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.Events;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Registration.Join;

using ErrorOr;

public static class JoinRegistrationHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(string id, 
        HttpContext context,
        //IMediator mediator, 
        AppDbContext dbContext,
        //IPublishEndpoint publishEndpoint,
        CancellationToken token)
    {

        var participantClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }
        
        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
        if (tournament is null)
        {
            return Error.NotFound();
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
        //await publishEndpoint.Publish(msg, token);

        return Result.Success;
    }
    
}