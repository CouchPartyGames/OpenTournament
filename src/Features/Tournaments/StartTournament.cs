using Microsoft.EntityFrameworkCore.Storage.Json;
using OpenTournament.Common;
using OpenTournament.Common.Models;
using OpenTournament.Common.Draw.Layout;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Common.Rules;
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

            var participants = _dbContext
                .Registrations
                .ToList();
                //.Find()
                //.Where(x => x.TournamentId == command.Id)
            
                // Apply Rules
            var engine = new RuleEngine();
            engine.Add(new TournamentInRegistrationState(tournament.Status));
            if (!engine.Evaluate())
            {
                return new RuleFailure(engine.Errors);
            }

            var opponents = new List<Opponent>();
            var order = ParticipantOrder.Order.Random;
            var participantOrder = ParticipantOrder.Create(order, opponents);
            DrawSize drawSize = DrawSize.CreateFromParticipants(participants.Count);

            var positions = new ParticipantPositions(drawSize);
            var localMatchIds = new LocalMatchIds(drawSize);
            var draw = new SingleEliminationDraw(positions, drawSize);
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
            _dbContext.Remove(participants); 
            
                // Update Tournament
            tournament.DrawSize = OpenTournament.Common.Models.DrawSize.Size8;
            tournament.Status = Status.InProcess;
            
                // Make Changes
            var results = await _dbContext.SaveChangesAsync(token);
                
            return true;
        }
    }
    
    
    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("/tournament/{id}/start", Endpoint)
            .WithTags("Tournament")
            .WithDescription("Start a tournament");
    

    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }
        
        var request = new StartTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(request, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            ruleErrs =>
            {
                return TypedResults.Problem("Rule Failures");
            });
    }
}