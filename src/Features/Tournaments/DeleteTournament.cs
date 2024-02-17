using Features.Matches;
using OneOf.Types;
using OpenTournament.Data;
using OpenTournament.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace Features.Tournaments;

public static class DeleteTournament
{
    public record DeleteTournamentCommand(TournamentId Id) : IRequest<OneOf<Success, OneOf.Types.NotFound>>;


    internal sealed class Handler : IRequestHandler<DeleteTournamentCommand, OneOf<Success, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext) => _dbContext = dbContext;
        
        public async ValueTask<OneOf<Success, OneOf.Types.NotFound>> Handle(DeleteTournamentCommand command, 
            CancellationToken token)
        {
            var tournament = _dbContext
                .Tournaments
                .FirstOrDefaultAsync(t => t.Id == command.Id);
            if (tournament is null)
            {
                return new OneOf.Types.NotFound();
            }
            
            _dbContext.Remove(tournament);
            var result = await _dbContext.SaveChangesAsync(token);
            if (result == 0)
            {
                
            }

            return new OneOf.Types.Success();
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("tournaments/{id}", Endpoint)
            .WithTags("Tournament")
            .WithSummary("Delete Tournament")
            .WithDescription("Delete an existing tournament")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, IMediator mediator, CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }
        
        var command = new DeleteTournamentCommand(new TournamentId(guid));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound>>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound());
    }
}