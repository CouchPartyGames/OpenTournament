using CouchPartyGames.TournamentGenerator;
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
        logger.LogInformation("Match Completed Consumer Started");
        
        var matchId = context.Message.MatchId;
        var tournamentId = context.Message.TournamentId;
        var winnerId = context.Message.WinnerId;
        var completedLocalMatchId = context.Message.CompletedLocalMatchId;
        
        var match = dbContext.Matches
            .Single(m => m.Id == matchId);

        var tournament = new SingleEliminationBuilder<MyOpponent>("Temporary")
            .SetSize(TournamentSize.Size4)
            .Build();
        
        var strategy = dbContext.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {

            var localMatch = tournament
                .Matches
                .Single(m => m.LocalMatchId == match.LocalMatchId);

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

    public bool HasNextMatchBeenCreated(List<Match> matches, int nextLocalMatchId) => 
        matches.Where(m => m.LocalMatchId == nextLocalMatchId).Count() == 0 ? false : true;

    /*
    public void CreateNextMatch(TournamentId tournamentId, CreateProgressionMatches.ProgressionMatch nextMatch, ParticipantId participantId) {
        var match = Match.CreateWithOneOpponent(tournamentId, nextMatch.MatchId, nextMatch.WinMatchId, participantId);
        dbContext.Add(match);
        dbContext.SaveChanges();
    }

    public void AssignOpponentToNextMatch(List<Match> matches, int nextLocalMatchId) {
        
        var match = matches
            .Where(r => r.Id == nextLocalMatchId)
            .Single();

        match.UpdateOpponent(match);
        dbContext.SaveChanges();
        
    }*/

    public bool IsTournamentComplete() {
        return false;
    }


    /*
    CreateProgressionMatches.ProgressionMatch FindLocalMatch(DrawSize drawSize, int localMatchId) {
        
            // Get Opponent Positions for the first round
        var positions = new FirstRoundPositions(drawSize);

            // Create Matches and Progressions
        var matchIds = new CreateMatchIds(positions);
        var progs = new CreateProgressionMatches(matchIds.MatchByIds);
        return progs
            .MatchWithProgressions
            .Where(p => p.MatchId == localMatchId)
            .Single();
    }

    CreateProgressionMatches.ProgressionMatch FindNextLocalMatch(DrawSize drawSize, int nextMatchId) {
            // Get Opponent Positions for the first round
        var positions = new FirstRoundPositions(drawSize);

            // Create Matches and Progressions
        var matchIds = new CreateMatchIds(positions);
        var progs = new CreateProgressionMatches(matchIds.MatchByIds);
        return progs
            .MatchWithProgressions
            .Where(p => p.MatchId == nextMatchId)
            .Single();
    }
    */
}