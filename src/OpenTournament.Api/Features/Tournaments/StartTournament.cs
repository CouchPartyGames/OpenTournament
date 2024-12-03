using CouchPartyGames.TournamentGenerator.Position;
using MassTransit;
using OpenTournament.Api.Common.Rules;
using OpenTournament.Api.Common.Rules.Tournaments;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Features.Tournaments;

public static class StartTournament
{

    public static async Task<Results<NoContent, NotFound, BadRequest, ProblemHttpResult>> Endpoint(string id,
        IMediator mediator,
        AppDbContext dbContext,
        ISendEndpointProvider sendEndpointProvider,
        CancellationToken token)
    {
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

        DrawSize drawSize = DrawSize.NewRoundBase2(participants.Count);

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            tournament.Start(drawSize);

            var msg = new TournamentStarted {
                TournamentId = tournamentId,
                DrawSize = (int)drawSize.Value,
                StartType = StartType.Manual
            };
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:tournament-started"));
            await endpoint.Send(msg, token);

            // Make Changes
            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        });

        return TypedResults.NoContent();
    }
}