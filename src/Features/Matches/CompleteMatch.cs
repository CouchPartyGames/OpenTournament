using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using OpenTournament.Data.Models;
using OpenTournament.Features;

namespace Features.Matches;

public static class CompleteMatch
{
    public sealed record CompleteMatchCommand(MatchId MatchId, ParticipantId WinnerId) : IRequest<Results<Ok, NotFound>>;

    internal sealed class
        Handler : IRequestHandler<CompleteMatchCommand, Results<Ok, NotFound>>
    {
        private readonly AppDbContext _dbContext;

        private readonly IAuthorizationService _authorizationService;

        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Results<Ok, NotFound>> Handle(CompleteMatchCommand command,
            CancellationToken token)
        {
            /*
            var matchId = MatchId.TryParse(id);
            if (matchId is null)
            {
                return TypedResults.ValidationProblem(ValidationErrors.MatchIdFailure);
            }*/
            
            var match = await _dbContext
                .Matches
                .FirstOrDefaultAsync(x => x.Id == command.MatchId, token);
            if (match is null) {
                return TypedResults.NotFound();
            }

            /*
            using var transaction = _dbContext.Database.BeginTransactionAsync();
            try
            {
                var createNextMatch = false;
                match.Complete();

                if (createNextMatch)
                {
                    var nextMatch = Match.Create(tournamentId);
                    _dbContext.AddAsync(nextMatch);
                }
                
                //new MatchCompletedEvent(matchId);

                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                transaction.RollbackAsync();
            }
            */
                

            return TypedResults.Ok();
        }
    }


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("matches/{id}/complete", (string id,
                CompleteMatchCommand command,
                IMediator mediator,
                CancellationToken token) =>
            {
                return Endpoint(id, command, mediator, token);
            })
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match")
            .WithOpenApi();
    
    public static async Task<Results<Ok, NotFound, ValidationProblem>> Endpoint(string id,
        CompleteMatchCommand command,
        IMediator mediator,
        CancellationToken token)
    {
        var result = await mediator.Send(command, token);
        return TypedResults.Ok();
        /*
        return result.Match<bool>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound()
            ); 
            */
    }
}