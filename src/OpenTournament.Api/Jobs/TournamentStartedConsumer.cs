using MassTransit;
using OpenTournament.Data.Models;
using OpenTournament.Data.DomainEvents;
using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;

namespace OpenTournament.Jobs;

public record Opponent(Guid Id, string Name, int Rank) : IOpponent;

public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger,
    AppDbContext dbContext) : IConsumer<TournamentStarted>
{
    public async Task Consume(ConsumeContext<TournamentStarted> context)
    {
        logger.LogInformation("Tournament Started Consumer");

        var tournamentId = context.Message.TournamentId;
        var numOpponents = context.Message.DrawSize;
        //var order = ParticipantOrder.Order.Ranked;
        
        //await ResilientTransaction.New(dbContext).ExecuteAsync(() =>
        //{
            var oppList = ConvertRegistrationsToParticipants(tournamentId);
            if (oppList.Count == 0)
            {
                logger.LogError("No opponent registered for this tournament");
            }
            var tournament = new SingleEliminationBuilder<Participant>("Temporary")
                .SetSize(DrawSize.NewRoundBase2(numOpponents).Value)
                .SetSeeding(TournamentSeeding.Ranked)
                .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
                .WithOpponents(oppList, Participant.CreateBye())
                .Build();


            var firstRoundMatches = tournament
                .Matches
                .Where(m => m.Round == 1)
                .ToList();
            if (firstRoundMatches.Count == 0)
            {
                logger.LogError($"Unable to find first round matches for {tournamentId}");
            }
            
            // Step - Create Matches
            foreach (var localMatch in firstRoundMatches)
            {
                var match = Match.New(tournamentId, localMatch.Opponent1.Id, localMatch.Opponent2.Id,
                    localMatch.WinProgression, localMatch.LocalMatchId);
                dbContext.Add(match);
            }
            // Achieving atomicity between original catalog database
            // operation and the IntegrationEventLog thanks to a local transaction
            await dbContext.SaveChangesAsync();
            
            //return Task.CompletedTask;
        //});
        //return Task.CompletedTask;
    }

    private List<Participant> ConvertRegistrationsToParticipants(TournamentId tournamentId) =>
        dbContext
            .Registrations
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();
}

public class DoStuff
{
    public Task Hell(AppDbContext dbContext, TournamentId tournamentId)
    {
        var oppList = dbContext
            .Registrations
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();
        
        return Task.CompletedTask;
    }
}