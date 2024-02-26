using OpenTournament.Data.Models;
using OpenTournament.Common.Draw.Layout;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using DrawSize = OpenTournament.Common.Draw.Layout.DrawSize;

namespace Features.Tournaments;

public static class StartTournament
{
    public sealed record StartTournamentCommand(TournamentId Id) : IRequest<OneOf<bool, OneOf.Types.NotFound, RuleFailure>>;

    internal sealed class Handler : IRequestHandler<StartTournamentCommand, OneOf<bool, OneOf.Types.NotFound, RuleFailure>>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

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
                .Participants
                .ToListAsync();
            
            var numParticipants = participants.Count;
            
                // Apply Rules
            var engine = new RuleEngine();
            engine.Add(new TournamentInRegistrationState(tournament.Status));
            engine.Add(new TournamentHasMinimumParticipants(numParticipants, tournament.MinParticipants));
            if (!engine.Evaluate())
            {
                return new RuleFailure(engine.Errors);
            }


            var opponents = new List<Opponent>();
            var order = ParticipantOrder.Order.Random;
            var participantOrder = ParticipantOrder.Create(order, participants);
            DrawSize drawSize = DrawSize.CreateFromParticipants(numParticipants);

            var positions = new FirstRoundPositions(drawSize);
            var localMatchIds = new LocalMatchIds(drawSize);
            var draw = new SingleEliminationDraw(positions);
            draw.CreateMatchProgressions(localMatchIds.CreateMatchIds());
            
                // Add Matches (1st Round)
            foreach(var drawMatch in draw.GetMatchesInRound(1))
            {
                var match = new Match();
                match.LocalMatchId = drawMatch.Id;
                match.State = MatchState.Ready;
                match.Id = MatchId.Create();
                //match.Opponent1 = drawMatch.Opponent1;
                //match.Opponent2 = drawMatch.Opponent2;
                _dbContext.Add(match);
            }

                // Clear Registration
            //_dbContext.Remove(participants); 
            
                // Update Tournament
            //tournament.DrawSize = drawSize;
            tournament.Status = Status.InProcess;
            
                // Make Changes
            var results = await _dbContext.SaveChangesAsync(token);
                
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
        var tournyId = TournamentId.TryParse(id);
        if (tournyId is null)
        {
            return TypedResults.NotFound();
        }
        
        var request = new StartTournamentCommand(tournyId);
        var result = await mediator.Send(request, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            ruleErrors =>
            {
                return TypedResults.Problem(title: "Rule Fggailures",
                    detail: ruleErrors.Errors[0].ToString(),
                    statusCode: StatusCodes.Status400BadRequest);
            });
    }
}