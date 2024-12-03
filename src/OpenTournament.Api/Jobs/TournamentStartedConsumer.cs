using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;
using CouchPartyGames.TournamentGenerator.Type;
using MassTransit;
using OneOf.Types;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Jobs;

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
                .WithOpponents(oppList, GlobalConstants.ByeOpponent)
                .Build();


            var firstRoundMatches = tournament
                .Matches
                .Where(m => m.Round == 1)
                .ToList();
            if (firstRoundMatches.Count == 0)
            {
                logger.LogError($"Unable to find first round matches for {tournamentId}");
            }
            
            // Step - Create First Round Matches
            foreach (var localMatch in firstRoundMatches)
            {
                var progression = GetProgressionFromLocalMatch(localMatch);
                var match = GetMatch(localMatch, tournamentId, GlobalConstants.ByeOpponent.Id, progression);
                dbContext.Add(match);

                // Add 2nd Round Match with Single Opponent
                if (Match.HasByeOpponent(localMatch, GlobalConstants.ByeOpponent))
                {
                    var participant = Match.GetNonByeOpponent(localMatch, GlobalConstants.ByeOpponent);
                    
                    var nextMatch = tournament.GetWinProgressionMatch(localMatch.LocalMatchId);
                    if (nextMatch is null) continue;
                    
                    
                    progression = GetProgressionFromLocalMatch(nextMatch);
                    match = Match.NewOneOpponent(tournamentId, nextMatch.LocalMatchId, progression, participant.Id);
                    dbContext.Add(match);
                }
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

    private Progression GetProgressionFromLocalMatch(Match<Participant> localMatch)
    {
        return localMatch switch
        {
            { WinProgression: Progression.NoProgression, LoseProgression: Progression.NoProgression } =>
                Progression.NewNoProgression(),
            { LoseProgression: Progression.NoProgression } => Progression.NewWin(localMatch.WinProgression),
            _ => Progression.NewWinLose(localMatch.WinProgression, localMatch.LoseProgression)
        };
    }

    private Match GetMatch(Match<Participant> localMatch, 
        TournamentId tournamentId,
        ParticipantId byeOpponentId,
        Progression progression)
    {
        if (localMatch.Opponent2.Id == byeOpponentId || localMatch.Opponent1.Id == byeOpponentId)
        {
            var winnerId = localMatch.Opponent2.Id;
            return Match.NewCompleted(tournamentId, localMatch, progression, Completion.New(winnerId));
        }

        return Match.NewUndetermined(tournamentId, localMatch, progression);
    }
}