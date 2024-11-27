using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Type;
using MassTransit;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace OpenTournament.Jobs;

public record MyOpponent(string Name, int Rank) : IOpponent;

public sealed class MatchCompletedConsumer(AppDbContext dbContext, 
    ILogger<MatchCompletedConsumer> logger) : IConsumer<MatchCompleted>
{
    public Task Consume(ConsumeContext<MatchCompleted> context)
    {
        Match nextDbMatch;
        Match completedDbMatch;
        
        logger.LogInformation("Match Completed Consumer Started");
        
        MatchId completedMatchId = context.Message.MatchId;
        TournamentId tournamentId = context.Message.TournamentId;
        ParticipantId winnerId = context.Message.WinnerId;
        var completedLocalMatchId = context.Message.CompletedLocalMatchId;

        completedDbMatch = dbContext.Matches.Single(x => x.Id == completedMatchId);
        var tournament = new SingleEliminationBuilder<Participant>("Temporary")
            .SetSize(TournamentSize.Size4)
            .Build();

        if (completedDbMatch.WinMatchId == Match.NoProgression)
        {
            return Task.CompletedTask;
        }
        
        nextDbMatch = dbContext
            .Matches
            .Where(x => x.TournamentId == tournamentId)
            .SingleOrDefault(x => x.LocalMatchId == completedDbMatch.WinMatchId);
        
        var strategy = dbContext.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {
            Match match;
            if (nextDbMatch == null)
            {
                
                // Single Elimination
                var localMatch = GetLocalMatch(FindNextLocalMatchId(completedLocalMatchId, tournament), tournament);
                var localMatchId = localMatch.LocalMatchId; 
                int nextMatchId = localMatch.WinProgression > 0 ? localMatch.WinProgression : Match.NoProgression;
                
                // Create Match
                match = Match.CreateWithOneOpponent(tournamentId, localMatchId, nextMatchId, winnerId);
                dbContext.Add(match);
            }
            else
            {
                // Add Second Opponent
                match = dbContext
                    .Matches
                    .Where(x => x.TournamentId == tournamentId)
                    .First(x => x.LocalMatchId == completedDbMatch.WinMatchId);
                
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

        logger.LogInformation("Match Completed Successful");
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