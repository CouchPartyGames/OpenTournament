using Microsoft.AspNetCore.Authorization;
using OneOf.Types;
using OpenTournament.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Features.Tournaments;

public static class DeleteTournament
{
    public record DeleteTournamentCommand(TournamentId Id) : IRequest<OneOf<Success, OneOf.Types.NotFound>>;


    internal sealed class Handler : IRequestHandler<DeleteTournamentCommand, OneOf<Success, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;

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
            
                // Check if allowed to delete
                /*
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, document, new EditRequirement());
            if (!authorizationResult.Succeeded)
            {
                return new OneOf.Types.Error<ForbidResult>();
            }*/

            _dbContext.Remove(tournament);
            var result = await _dbContext.SaveChangesAsync(token);
            if (result == 0)
            {
                
            }

            return new OneOf.Types.Success();
        }
    }


    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, IMediator mediator, CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }

        var command = new DeleteTournamentCommand(tournamentId);
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound>>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound());
    }
}