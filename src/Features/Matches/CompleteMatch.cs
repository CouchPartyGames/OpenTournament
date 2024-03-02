using Microsoft.AspNetCore.Authorization;
using OpenTournament.Data.Models;

namespace Features.Matches;

public static class CompleteMatch
{
    public sealed record CompleteMatchCommand(MatchId MatchId) : IRequest<bool>;

    internal sealed class
        Handler : IRequestHandler<CompleteMatchCommand, bool>
    {
        private readonly AppDbContext _dbContext;

        private readonly IAuthorizationService _authorizationService;

        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<bool> Handle(CompleteMatchCommand command,
            CancellationToken token)
        {
            /*
            var match = await _dbContext.Matches.FirstOrDefaultAsync(x => x.Id == matchId);

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

                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                transaction.RollbackAsync();
            }
            */
                

            return true;
        }
    }


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("matches/{id}/complete", (string id,
                CompleteMatchCommand cmd,
                IMediator mediator,
                CancellationToken token) =>
            {
                return Endpoint(id, cmd, mediator, token);
            })
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match")
            .WithOpenApi();
    
    public static async Task<Results<Ok, NotFound, ValidationProblem>> Endpoint(string id,
            CompleteMatchCommand request,
            IMediator mediator,
            CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }

        var command = new CompleteMatchCommand(new MatchId(guid));
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