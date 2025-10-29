using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using OpenTournament.Core.Rules;
using OpenTournament.Core.Rules.Tournaments;

namespace OpenTournament.Core.Features.Tournaments.Start;
using ErrorOr;
public static class StartTournamentHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        string id,
        //IMediator mediator,
        AppDbContext dbContext,
        //ISendEndpointProvider sendEndpointProvider,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }

        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
        if (tournament is null)
        {
            return Error.NotFound();
        }

        var participants = await dbContext
            .Registrations
            .Where(x => x.TournamentId == tournament.Id)
            .Select(x => x.Participant)
            .ToListAsync(token);


        // Apply Rules
        var engine = new RuleEngine();
        engine.Add(new TournamentInRegistrationState(tournament.Status));
        engine.Add(new TournamentHasMinimumParticipants(participants.Count, tournament.MinParticipants));
        if (!engine.Evaluate())
        {
            return Error.Failure();
            //return TypedResults.ValidationProblem(engine.ToValidationExtensions());
        }

        DrawSize drawSize = DrawSize.NewRoundBase2(participants.Count);

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            tournament.Start(drawSize);

            /*
            var msg = new TournamentStarted {
                TournamentId = tournamentId,
                DrawSize = (int)drawSize.Value,
                StartType = StartType.Manual
            };
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:tournament-started"));
            await endpoint.Send(msg, token);
            */
            var oppList = ConvertRegistrationsToParticipants(tournamentId, dbContext);

            var tournamentSingle = new SingleEliminationBuilder<Participant>("Temporary")
                .SetSize(DrawSize.NewRoundBase2((int)drawSize.Value).Value)
                .SetSeeding(TournamentSeeding.Ranked)
                .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
                .WithOpponents(oppList, GlobalConstants.ByeOpponent)
                .Build();

            var tournamentMatches = new TournamentMatches()
            {
                TournamentId = tournamentId,
                Matches = []
            };
            foreach (var match in tournamentSingle.Matches)
            {
                var someMatch = new MatchMetadata()
                {
                    MatchId = MatchId.NewMatchId(),
                    LocalMatchId = match.LocalMatchId,
                    MatchState = GetMatchState(match.Opponent1, match.Opponent2),
                    MatchParticipants = GetParticipants(match.Opponent1.Id, match.Opponent2.Id)
                };
                tournamentMatches.Matches.Add(someMatch);
            }

            dbContext.Add(tournamentMatches);

            // Make Changes
            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        });

        return Result.Success;
    }

    private static List<Participant> ConvertRegistrationsToParticipants(TournamentId tournamentId,
        AppDbContext dbContext) =>
        dbContext
            .Registrations
            .AsNoTracking()
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToList();

    private static MatchMetadata.State GetMatchState(Participant p1, Participant p2)
    {
        if (p1.Id == GlobalConstants.ByeOpponent.Id || p2.Id == GlobalConstants.ByeOpponent.Id)
        {
            return MatchMetadata.State.Completed;
        } else if (p1.Id != GlobalConstants.ByeOpponent.Id && p2.Id != GlobalConstants.ByeOpponent.Id)
        {
            return MatchMetadata.State.Ready;
        }
        return MatchMetadata.State.Waiting;
    }
    
    private static Dictionary<int, ParticipantId> GetParticipants(ParticipantId opp1, ParticipantId opp2)
    {
        if (opp1 != null && opp2 != null)
        {
            return new() { {0, opp1}, {1, opp2} };
        }
        
        return new();
    }
    
}