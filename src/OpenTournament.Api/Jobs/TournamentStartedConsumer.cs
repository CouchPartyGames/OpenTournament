using MassTransit;
using OpenTournament.Data.Models;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Draw.Layout;

namespace Jobs;

public sealed class TournamentStartedConsumer(ILogger<TournamentStartedConsumer> logger,
    AppDbContext dbContext) : IConsumer<TournamentStarted>
{
    public Task Consume(ConsumeContext<TournamentStarted> context)
    {
        logger.LogInformation("Tournament Started Consumer");

        var tournamentId = context.Message.TournamentId;
        var drawSize = context.Message.DrawSize;
        var order = ParticipantOrder.Order.Ranked;

            // Step - Get Opponents from Registration
        var participantList = ConvertRegistrationsToParticipants(tournamentId);
        var participants = ParticipantOrder.Create(order, participantList);

            // Step - Create Draw/Positions
        var matches = CreateFirstRoundMatches(drawSize, participants);

            // Step - Create Matches
        foreach(var singleMatch in matches) {
            var match = Match.Create(tournamentId, singleMatch);
            dbContext.Add(match);
        }
        dbContext.SaveChanges();

        return Task.CompletedTask;
    }

    private List<Participant> ConvertRegistrationsToParticipants(TournamentId tournamentId) =>
        dbContext
            .Registrations
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();
    

    private List<SingleEliminationFirstRound.SingleMatch> CreateFirstRoundMatches(DrawSize drawSize, ParticipantOrder order) {
            // Get Opponent Positions for the first round
        var positions = new FirstRoundPositions(drawSize);

            // Create Matches and Progressions
        var matchIds = new CreateMatchIds(positions);
        var progs = new CreateProgressionMatches(matchIds.MatchByIds);

            // Tie all together
        var draw = new SingleEliminationFirstRound(progs.MatchWithProgressions, order);
        return draw.Matches;
    }
}