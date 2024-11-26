using MassTransit;
using OpenTournament.Data.Models;
using OpenTournament.Data.DomainEvents;
using CouchPartyGames.TournamentGenerator;

namespace OpenTournament.Jobs;

public record Opponent(Guid Id, string Name, int Rank) : IOpponent;

public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger,
    AppDbContext dbContext) : IConsumer<TournamentStarted>
{
    public Task Consume(ConsumeContext<TournamentStarted> context)
    {
        logger.LogInformation("Tournament Started Consumer");

        var tournamentId = context.Message.TournamentId;
        var drawSize = context.Message.DrawSize;
        //var order = ParticipantOrder.Order.Ranked;
        
        var oppList = ConvertRegistrationsToParticipants(tournamentId);
        /*
        var tournament = new SingleEliminationBuilder<Opponent>("Temporary")
            .SetSize(TournamentSize.Size4)
            .SetSeeding(TournamentSeeding.Ranked)
            .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
            .WithOpponents(oppList, new Opponent(Guid.CreateVersion7(), "Bye", int.MaxValue))
            .Build();
        
        var firstRoundMatches = tournament
            .Matches
            .Where(m => m.Round == 1)
            .Select(m => new SingleEliminationFirstRound.SingleMatch(m.Round, m.LocalMatchId, ))
            .ToList();

            // Step - Create Matches
        foreach(var singleMatch in firstRoundMatches) {
            var match = Match.Create(tournamentId, singleMatch);
            dbContext.Add(match);
        }
        dbContext.SaveChanges();
        */

        return Task.CompletedTask;
    }

    private List<Participant> ConvertRegistrationsToParticipants(TournamentId tournamentId) =>
        dbContext
            .Registrations
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();
    

}