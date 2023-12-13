using Features.Matches;
using OpenTournament.Common;
using OpenTournament.Common.Rules;

namespace Features.Tournaments;

public static class JoinTournament
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
            //engine.Add(new TournamentStatusIsRegistration());
            //engine.Add(new TournamentNotInProgress());
            if (!engine.Evaluate())
            {
                //engine.Errors();
            }
            
                // Add User/Participant
            var result = await _dbContext.SaveChangesAsync(token);

            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) => 
        app.MapPut("tournaments/{id}/join", Endpoint);

    
    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Endpoint(string id, 
        IMediator mediator, 
        CancellationToken token)
    {
        Guid.TryParse(id, out Guid guid);
        var command = new JoinTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound, ProblemHttpResult>> (
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            errors =>
            {
                return TypedResults.Problem();
            });
    }
}