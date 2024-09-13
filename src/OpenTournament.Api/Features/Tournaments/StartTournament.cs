using OpenTournament.Data.Models;
using OpenTournament.Common.Draw.Layout;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using OpenTournament.Data.DomainEvents;
using MassTransit;

namespace OpenTournament.Features.Tournaments;

public static class StartTournament
{

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("/tournaments/{id}/start", Endpoint)
            .WithTags("Tournament")
            .WithSummary("Start Tournament")
            .WithDescription("Mark the tournament as ready to begin")
            .WithOpenApi()
            .RequireAuthorization();
    

    public static async Task<Results<NoContent, NotFound, BadRequest, ProblemHttpResult>> Endpoint(string id,
        IMediator mediator,
        AppDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        
        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId);
        if (tournament is null)
        {
            return TypedResults.NotFound();
        }

        var participants = await dbContext
            .Registrations
            .Where(x => x.TournamentId == tournament.Id)
            .Select(x => x.Participant)
            .ToListAsync(token);
        
            
            // Apply Rules
        var engine = new RuleEngine();
        engine.Add(new TournamentInRegistrationState(tournament.Status));
        engine.Add(new TournamentHasMinimumParticipants(participants.Count, tournament.MinParticipants));
        if (!engine.Evaluate())
        {
            return TypedResults.BadRequest();
        }


        var order = ParticipantOrder.Order.Random;
        var participantOrder = ParticipantOrder.Create(order, participants);
        DrawSize drawSize = DrawSize.CreateFromParticipants(participants.Count);


        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            tournament.Start(drawSize);

            var msg = new TournamentStarted {
                TournamentId = tournamentId,
                DrawSize = drawSize
            };
            await publishEndpoint.Publish(msg, token);

            // Make Changes
            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        });

        return TypedResults.NoContent();
    }
}