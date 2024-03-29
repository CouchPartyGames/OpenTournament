using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class JoinRegistration
{
    public sealed record JoinTournamentCommand(TournamentId TournamentId, ParticipantId ParticipantId) : IRequest<OneOf<bool, OneOf.Types.NotFound, RuleFailure>>;

    internal sealed class Handler : IRequestHandler<JoinTournamentCommand, OneOf<bool, OneOf.Types.NotFound, RuleFailure>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<bool, OneOf.Types.NotFound, RuleFailure>> Handle(JoinTournamentCommand command, 
            CancellationToken token)
        {
            
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(m => m.Id == command.TournamentId, token);
            if (tournament is null)
            {
                return new OneOf.Types.NotFound();
            }

                // Rules
            var engine = new RuleEngine();
            engine.Add(new TournamentInRegistrationState(tournament.Status));
            if (!engine.Evaluate())
            {
                return new RuleFailure(engine.Errors);
            }

            //new JoinedTournamentEvent(command.TournamentId, command.ParticipantId);
            _dbContext.Add(Registration.Create(command.TournamentId, command.ParticipantId));
            var result = await _dbContext.SaveChangesAsync(token);
            if (result < 1)
            {
            }

            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("registrations/{id}/join", Endpoint)
            .WithTags("Registration")
            .WithSummary("Join Tournament")
            .WithDescription("Join a tournament")
            .WithOpenApi()
            .RequireAuthorization();

    
    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Endpoint(string id, 
        HttpContext context,
        IMediator mediator, 
        CancellationToken token)
    {

        var participantId = context.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        
        var command = new JoinTournamentCommand(tournamentId, new ParticipantId(participantId?.Value));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            errors =>
            {
                return TypedResults.Problem(errors.Errors.FirstOrDefault()?.Message, title: "invalid state", statusCode: StatusCodes.Status409Conflict);
            });
    }
}