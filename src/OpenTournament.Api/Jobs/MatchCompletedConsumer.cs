using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;
using CouchPartyGames.TournamentGenerator.Type;
using MassTransit;
using OpenTournament.Api.Common.Extensions;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Jobs;


public sealed class MatchCompletedConsumer(AppDbContext dbContext, 
    ILogger<MatchCompletedConsumer> logger) : IConsumer<MatchCompleted>
{
    public Task Consume(ConsumeContext<MatchCompleted> context)
    {
        MatchCompletedLog.ConsumerStarted(logger, context.Message.MatchId);
        
        Match nextDbMatch;
        Match completedDbMatch;
        
        
        MatchId completedMatchId = context.Message.MatchId;
        TournamentId tournamentId = context.Message.TournamentId;
        ParticipantId winnerId = context.Message.WinnerId;
        var completedLocalMatchId = context.Message.CompletedLocalMatchId;

        var dbTournament = dbContext
            .Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId).Result;

        int drawSize = 4;

        completedDbMatch = dbContext.Matches.Include(match => match.Progression).Single(x => x.Id == completedMatchId);
        var tournament = new SingleEliminationBuilder<Participant>("Temporary")
            .SetSize(DrawSize.NewRoundBase2(drawSize).Value)
            .Build();

        if (completedDbMatch.Progression.WinProgressionId == Progression.NoProgression)
        {
            return Task.CompletedTask;
        }
        
        nextDbMatch = dbContext
            .Matches
            .Where(x => x.TournamentId == tournamentId)
            .SingleOrDefault(x => x.LocalMatchId == completedDbMatch.Progression.WinProgressionId);
        
        var strategy = dbContext.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {
            Match match;
            if (nextDbMatch == null)
            {
                
                // Single Elimination
                var localMatch = GetLocalMatch(FindNextLocalMatchId(completedLocalMatchId, tournament), tournament);
                var localMatchId = localMatch.LocalMatchId; 
                int nextMatchId = localMatch.WinProgression > 0 ? localMatch.WinProgression : Progression.NoProgression;

                var progression = localMatch.GetProgression();
                
                // Create Match
                match = Match.NewOneOpponent(tournamentId, localMatchId, progression, winnerId);
                dbContext.Add(match);
            }
            else
            {
                // Add Second Opponent
                match = dbContext
                    .Matches
                    .Where(x => x.TournamentId == tournamentId)
                    .First(x => x.LocalMatchId == completedDbMatch.Progression.WinProgressionId);
                
                match.UpdateOpponent(winnerId);
            }
            dbContext.SaveChangesAsync();
            
            // Double Elimination
            
            /*
                // Current Next Match
            if (localMatch.NextWinProgressionExists()) {

                var nextLocalMatch = FindNextLocalMatch(win);
                    // Yes
                if (HasNextMatchBeenCreated(matches, nextLocalMatchId)) {
                    AssignOpponentToNextMatch(matches, nextLocalMatchId);
                } else {
                    CreateNextMatch(tournamentId, nextLocalMatch, winnerId);
                }
            } else {
                // Is Tournament Complete
                if (IsTournamentComplete()) {
                    // Notify others tournament is complete
                }
            }*/
        }); 
        
        MatchCompletedLog.ConsumerSuccessful(logger, context.Message.MatchId);
        return Task.CompletedTask;
    }


    Match<Participant>? GetLocalMatch(int localMatchId, Tournament<Participant> tournament)
    {
        if (localMatchId < 1)
        {
            return null;
        } 
        
       return tournament.Matches.First(x => x.LocalMatchId == localMatchId);
    }
    
    int FindNextLocalMatchId(int localMatchId, Tournament<Participant> tournament)
    {
        var nextLocalMatchId = tournament.Matches
            .Where(m => m.LocalMatchId == localMatchId)
            .Select(x => x.WinProgression)
            .Single();
        
        return nextLocalMatchId;
    }
    
}


public static partial class MatchCompletedLog
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Starting Match Completed consumer `{matchId}`")]
    public static partial void ConsumerStarted(ILogger logger, MatchId matchId);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Match Completed consumer failed {reason} `{matchId}`")]
    public static partial void ConsumerFailed(ILogger logger, MatchId matchId, string reason);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Match Completed successfully `{matchId}`")]
    public static partial void ConsumerSuccessful(ILogger logger, MatchId matchId);
}
