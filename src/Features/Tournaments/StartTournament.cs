using OneOf.Types;
using OpenTournament.Common;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace Features.Tournaments;

public static class StartTournament
{
    public sealed record StartTournamentCommand(TournamentId Id) : IRequest<bool>;

    internal sealed class Handler : IRequestHandler<StartTournamentCommand, bool>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<bool> Handle(StartTournamentCommand command,
            CancellationToken token)
        {
            var tournament = await _dbContext
                .Tournaments
                .FirstOrDefaultAsync(m => m.Id == command.Id);

            // Apply Rules
            
            // Save
            tournament.Status = Status.InProcess;
            var results = await _dbContext.SaveChangesAsync(token);
                
            return true;
        }
    }
    
    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("/tournament/{id}/start", Endpoint);

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        Guid.TryParse(id, out Guid guid);
        var request = new StartTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(request, token);
        return TypedResults.NoContent();
    }
}