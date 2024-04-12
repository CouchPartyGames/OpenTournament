using Microsoft.AspNetCore.Authorization;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace OpenTournament.Features.Matches;

public static class CompleteMatch
{
    public sealed record CompleteMatchCommand(MatchId MatchId, ParticipantId WinnerId) : IRequest<OneOf<Ok, NotFound>>;

    internal sealed class
        Handler : IRequestHandler<CompleteMatchCommand, OneOf<Ok, NotFound>>
    {
        private readonly AppDbContext _dbContext;

        private readonly IAuthorizationService _authorizationService;

        public Handler(AppDbContext dbContext, IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
        }

        public async ValueTask<OneOf<Ok, NotFound>> Handle(CompleteMatchCommand command,
            CancellationToken token)
        {
            /*
            // Authorize Dedicated Hosts and Tournament Moderators
            var authorizationResult = await _authorizationService.AuthorizeAsync(User);
            if (!authorizationResult.Succeeded)
            {
               return Forbid(); 
            }*/
                
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

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(token);
            try
            {
                match.Complete(command.WinnerId);

                await _dbContext.AddAsync(
                    Outbox.Create("match.completed", 
                        new MatchCompletedEvent(command.MatchId, match.TournamentId)),
                    token);

                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync(token);
                return TypedResults.NotFound();
            }

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
        return result.Match<Results<Ok, NotFound, ValidationProblem>>(
            _ => TypedResults.Ok(),
            _ => TypedResults.NotFound()
            ); 
    }
}