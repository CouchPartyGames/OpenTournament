using Features.Matches;
using OpenTournament.Common.Rules;
using OpenTournament.Models;

namespace Features.Tournaments;

public static class JoinRegistration
{
    public sealed record JoinTournamentCommand(TournamentId Id) : IRequest<OneOf<bool, OneOf.Types.NotFound, RuleFailure>>;

    internal sealed class Handler : IRequestHandler<JoinTournamentCommand, OneOf<bool, OneOf.Types.NotFound, RuleFailure>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<bool, OneOf.Types.NotFound, RuleFailure>> Handle(JoinTournamentCommand command, 
            CancellationToken token)
        {
            
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(m => m.Id == command.Id);
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
            
                // Add User/Participant
            var registration = new Registration
            {
                TournamentId = tournament.Id,
                ParticipantId = new ParticipantId(Guid.NewGuid())
            };
            
            _dbContext.Add(registration);
            var result = await _dbContext.SaveChangesAsync(token);
            if (result < 1)
            {
            }

            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("registrations/{id}/join", Endpoint).WithTags("Registration");

    
    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Endpoint(string id, 
        IMediator mediator, 
        CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }
        
        var command = new JoinTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            errors =>
            {
                return TypedResults.Problem("Rule Failure");
            });
    }
}