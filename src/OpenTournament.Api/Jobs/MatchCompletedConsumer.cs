using MassTransit;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Draw.Layout;

namespace Jobs;

public sealed class MatchCompletedConsumer(AppDbContext dbContext, 
    ILogger<MatchCompletedConsumer> logger) : IConsumer<MatchCompleted>
{
    public Task Consume(ConsumeContext<MatchCompleted> context)
    {
        logger.LogInformation("Match Completed Consumer");

        var matchId = context.Message.MatchId;
        var tournamentId = context.Message.TournamentId;

        var matches = dbContext
            .Matches
            .Where(m => m.TournamentId == tournamentId)
            .ToList();

        var tournament = dbContext
            .Tournaments
            .Where(t => t.Id == tournamentId);

            // Find Next Match
        //var curProgressionMatch = FindLocalMatch(tournament.drawSize, match.LocalMatchId);


            // Create Next Match

        throw new NotImplementedException();
        return Task.CompletedTask;
    }

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
        return progs.MatchWithProgressions
            .Where(p => p.MatchId == localMatchId);
    }*/
}