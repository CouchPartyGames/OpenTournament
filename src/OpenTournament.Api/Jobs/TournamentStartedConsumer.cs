using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;
using CouchPartyGames.TournamentGenerator.Type;
using MassTransit;
using OpenTournament.Api.Common.Extensions;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Jobs;


public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger,
    AppDbContext dbContext) : IConsumer<TournamentStarted>
{
    public async Task Consume(ConsumeContext<TournamentStarted> context)
    {
        TournamentStartedLog.ConsumerStarted(logger, context.Message.TournamentId);

        var tournamentId = context.Message.TournamentId;
        var numOpponents = context.Message.DrawSize;
        
        //await ResilientTransaction.New(dbContext).ExecuteAsync(() =>
        //{
            var oppList = ConvertRegistrationsToParticipants(tournamentId);
            if (oppList.Count == 0)
            {
                TournamentStartedLog.ConsumerFailed(logger, context.Message.TournamentId, "No opponents were found");
                return;
                //return Task.CompletedTask;
            }
            var tournament = new SingleEliminationBuilder<Participant>("Temporary")
                .SetSize(DrawSize.NewRoundBase2(numOpponents).Value)
                .SetSeeding(TournamentSeeding.Ranked)
                .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
                .WithOpponents(oppList, GlobalConstants.ByeOpponent)
                .Build();

            /* GDO-414
            var tournamentMatches = new TournamentMatches()
            {
                TournamentId = tournamentId,
                Matches =
                [
                    new MatchMetadata
                    {
                        MatchId = MatchId.NewMatchId(),
                        MatchState = MatchMetadata.State.Ready,
                        Metadata = {}
                    }
                ]
            };
            dbContext.Add(tournamentMatches);
            */

            var firstRoundMatches = tournament
                .Matches
                .Where(m => m.Round == 1)
                .ToList();
            if (firstRoundMatches.Count == 0)
            {
                TournamentStartedLog.ConsumerFailed(logger, context.Message.TournamentId, "Unable to find first round matches");
                return;
            }
            
            // Step - Create First Round Matches
            foreach (var localMatch in firstRoundMatches)
            {
                var progression = localMatch.GetProgression();
                var match = GetMatch(localMatch, tournamentId, GlobalConstants.ByeOpponent, progression);
                dbContext.Add(match);

                // Add 2nd Round Match with Single Opponent
                if (Match.HasByeOpponent(localMatch, GlobalConstants.ByeOpponent))
                {
                    var participant = Match.GetNonByeOpponent(localMatch, GlobalConstants.ByeOpponent);
                    
                    var nextMatch = tournament.GetWinProgressionMatch(localMatch.LocalMatchId);
                    if (nextMatch is null) continue;


                    progression = nextMatch.GetProgression(); 
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
        TournamentStartedLog.ConsumerSuccessful(logger, context.Message.TournamentId);
    }

    private List<Participant> ConvertRegistrationsToParticipants(TournamentId tournamentId) =>
        dbContext
            .Registrations
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();


    private Match GetMatch(Match<Participant> localMatch, 
        TournamentId tournamentId,
        Participant byeOpponent,
        Progression progression)
    {
        if (localMatch.HasByeOpponent(byeOpponent))
        {
            var winnerId = byeOpponent.Id == localMatch.Opponent2.Id
                ? localMatch.Opponent1.Id
                : localMatch.Opponent2.Id;
            return Match.NewCompleted(tournamentId, localMatch, progression, Completion.New(winnerId));
        }

        return Match.NewUndetermined(tournamentId, localMatch, progression);
    }
}

public static partial class TournamentStartedLog {
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Tournament Started consumer running `{tournamentId}`")]
    public static partial void ConsumerStarted(ILogger logger, TournamentId tournamentId);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Tournament Started consumer failed {message} `{tournamentId}`")]
    public static partial void ConsumerFailed(ILogger logger, TournamentId tournamentId, string message);
    
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Tournament Started consumer successfully completed `{tournamentId}`")]
    public static partial void ConsumerSuccessful(ILogger logger, TournamentId tournamentId);
}