using MassTransit;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Jobs;

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

        var strategy = dbContext.Database.CreateExecutionStrategy();
        strategy.Execute(() => {

            var matches = dbContext
                .Matches
                .Where(m => m.TournamentId == tournamentId)
                .ToList();

            var tournament = dbContext
                .Tournaments
                .First(t => t.Id == tournamentId);

            var drawSize = DrawSize.CreateFromParticipants((int)tournament.DrawSize);

                // Current Next Match
            var curCompletedMatch = FindLocalMatch(drawSize, completedLocalMatchId);
            Console.WriteLine(curCompletedMatch);

            if (IsThereANextMatch(curCompletedMatch)) {
                var nextLocalMatchId = curCompletedMatch.WinMatchId;

                var nextLocalMatch = FindNextLocalMatch(drawSize, nextLocalMatchId);
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
            }
        }); 

        logger.LogInformation("Match Completed Successful");
        return Task.CompletedTask;
    }


    public bool IsThereANextMatch(CreateProgressionMatches.ProgressionMatch currentMatch) => 
        currentMatch.WinMatchId > currentMatch.MatchId ? true : false;


    public bool HasNextMatchBeenCreated(List<Match> matches, int nextLocalMatchId) => 
        matches.Where(m => m.LocalMatchId == nextLocalMatchId).Count() == 0 ? false : true;

    public void CreateNextMatch(TournamentId tournamentId, CreateProgressionMatches.ProgressionMatch nextMatch, ParticipantId participantId) {
        var match = Match.CreateWithOneOpponent(tournamentId, nextMatch.MatchId, nextMatch.WinMatchId, participantId);
        dbContext.Add(match);
        dbContext.SaveChanges();
    }

    public void AssignOpponentToNextMatch(List<Match> matches, int nextLocalMatchId) {
        
        /*
        var match = matches
            .Where(r => r.Id == nextLocalMatchId)
            .Single();

        match.UpdateOpponent(match);
        dbContext.SaveChanges();
        */
        
    }

    public bool IsTournamentComplete() {
        return false;
    }


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
}