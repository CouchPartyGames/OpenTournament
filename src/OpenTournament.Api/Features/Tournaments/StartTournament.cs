using OpenTournament.Data.Models;
using OpenTournament.Common.Draw.Layout;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using OpenTournament.Data.DomainEvents;

namespace OpenTournament.Features.Tournaments;

public static class StartTournament
{
    public sealed record StartTournamentCommand(TournamentId Id) : IRequest<OneOf<bool, OneOf.Types.NotFound, RuleFailure>>;

    internal sealed class Handler : IRequestHandler<StartTournamentCommand, OneOf<bool, OneOf.Types.NotFound, RuleFailure>>
    {
        private readonly AppDbContext _dbContext;
        //private readonly Logger<Handler> _logger;

        public Handler(AppDbContext dbContext /*, Logger<Handler> logger*/)
        {
            _dbContext = dbContext;
//            _logger = logger;
        }

        public async ValueTask<OneOf<bool, OneOf.Types.NotFound, RuleFailure>> Handle(StartTournamentCommand command,
            CancellationToken token)
        {
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(m => m.Id == command.Id);
            if (tournament is null)
            {
                return new OneOf.Types.NotFound();
            }

            var participants = await _dbContext
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
                return new RuleFailure(engine.Errors);
            }


            var order = ParticipantOrder.Order.Random;
            var participantOrder = ParticipantOrder.Create(order, participants);
            DrawSize drawSize = DrawSize.CreateFromParticipants(participants.Count);


            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.Execute(async () =>
            {
                //try {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(token);
                tournament.Start(drawSize);

                await _dbContext.AddAsync(Outbox.Create(
                    new TournamentStartedEvent(tournament.Id, drawSize)), token);

                // Make Changes
                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                /*
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync(token);

                    //_logger.LogError("Failed transaction, triggered rollback");
                    // To Do, handle failure
                    Console.WriteLine(e);
                    return false;
                }*/
            });

            return true;
        }
    }


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("/tournaments/{id}/start", Endpoint)
            .WithTags("Tournament")
            .WithSummary("Start Tournament")
            .WithDescription("Mark the tournament as ready to begin")
            .WithOpenApi()
            .RequireAuthorization();
    

    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            //return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
            return TypedResults.NotFound();
        }
        
        var request = new StartTournamentCommand(tournamentId);
        var result = await mediator.Send(request, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            ruleErrors =>
            {
                return TypedResults.Problem(title: "Rule Failures",
                    detail: ruleErrors.Errors[0].ToString(),
                    statusCode: StatusCodes.Status400BadRequest);
            });
    }
}