using Features.Matches;
using OpenTournament.Common;

namespace Features.Tournaments;

public static class JoinTournament
{
    public sealed record JoinTournamentCommand(TournamentId Id) : IRequest<bool>;

    internal sealed class Handler : IRequestHandler<JoinTournamentCommand, bool>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<bool> Handle(JoinTournamentCommand command, 
            CancellationToken token)
        {

            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) => 
        app.MapPut("tournaments/{id}/join", Endpoint);

    
    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, 
        IMediator mediator, 
        CancellationToken token)
    {
        Guid.TryParse(id, out Guid guid);
        var command = new JoinTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(command, token);
        return TypedResults.NoContent();
    }
}