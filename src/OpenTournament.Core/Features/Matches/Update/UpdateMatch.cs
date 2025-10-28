using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Matches;

public static class UpdateMatch
{
    public sealed record UpdateMatchCommand(MatchId Id) : IRequest<OneOf<bool, OneOf.Types.NotFound, OneOf.Types.None>>;


    internal sealed class Handler : IRequestHandler<UpdateMatchCommand, OneOf<bool, OneOf.Types.NotFound, OneOf.Types.None>>
    {
        private readonly AppDbContext _dbContext;

        private readonly IAuthorizationService _authorizationService;

        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async ValueTask<OneOf<bool, OneOf.Types.NotFound, OneOf.Types.None>> Handle(UpdateMatchCommand command,
            CancellationToken token)
        {
            var match = await _dbContext
                .Matches
                .FirstOrDefaultAsync(m => m.Id == command.Id, token);
            if (match is null)
            {
                return new OneOf.Types.NotFound();
            }
            
            /* Is user allowed to update match
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, document, new EditRequirement());
            if (!authorizationResult.Succeeded)
            {
                return new OneOf.Types.Error<>();
            }*/
            
            //match.Status
            var result = await _dbContext.SaveChangesAsync(token);
            if (result == 0)
            {
                return new OneOf.Types.None();
            }
            return true;
        }
        
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Endpoint(string id,
        UpdateMatchCommand request,
        IMediator mediator,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(id);
        if (matchId is null)
        { 
            return TypedResults.ValidationProblem(ValidationErrors.MatchIdFailure);
        }

        var command = request with { Id = matchId };
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound, ValidationProblem>>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            _ => TypedResults.NotFound());
    }
}