﻿using MassTransit;
using Microsoft.AspNetCore.Authorization;
using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace OpenTournament.Features.Matches;

public static class CompleteMatch
{
    public sealed record CompleteMatchCommand(string MatchId, string WinnerId) : IRequest<OneOf<Ok, NotFound>>;

    internal sealed class
        Handler : IRequestHandler<CompleteMatchCommand, OneOf<Ok, NotFound>>
    {
        private readonly AppDbContext _dbContext;

        private readonly IAuthorizationService _authorizationService;

        private readonly IBus _bus;

        public Handler(AppDbContext dbContext, IAuthorizationService authorizationService, IBus bus)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _bus = bus;
        }

        public async ValueTask<OneOf<Ok, NotFound>> Handle(CompleteMatchCommand command,
            CancellationToken token)
        {
            var matchId = MatchId.TryParse(command.MatchId);
            var winnerId = new ParticipantId(command.WinnerId);
            /*
            // Authorize Dedicated Hosts and Tournament Moderators
            var authorizationResult = await _authorizationService.AuthorizeAsync(User);
            if (!authorizationResult.Succeeded)
            {
               return Forbid(); 
            }*/
                
            if (matchId is null)
            {
                Console.WriteLine("bad id");
                return TypedResults.NotFound();
//                return TypedResults.ValidationProblem(ValidationErrors.MatchIdFailure);
            }
            
            var match = await _dbContext
                .Matches
                .FirstOrDefaultAsync(x => x.Id == matchId, token);


            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.Execute(async () =>
            {

                await using var transaction = await _dbContext.Database.BeginTransactionAsync(token);

                match.Complete(winnerId);
                var msg = new MatchCompleted {
                    MatchId = matchId,
                    TournamentId = match.TournamentId,
                    WinnerId = match.WinnerId,
                    CompletedLocalMatchId = match.LocalMatchId
                };
                await _bus.Send(msg);

                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            });

            return TypedResults.Ok();
        }
    }


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("matches/{id}/complete", (string id,
                CompleteMatchCommand command,
                ISendEndpointProvider sendEndpointProvider,
                AppDbContext dbContext,
                CancellationToken token) =>
            {
                return Endpoint(id, command, sendEndpointProvider, dbContext, token);
            })
            .WithTags("Match")
            .WithSummary("Complete Match")
            .WithDescription("Complete an Individual Match")
            .WithOpenApi();

    private static async Task<Results<Ok, NotFound, ValidationProblem>> Endpoint(string id,
        CompleteMatchCommand command,
        ISendEndpointProvider sendEndpointProvider,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var matchId = MatchId.TryParse(command.MatchId);
        var winnerId = new ParticipantId(command.WinnerId);

        /*
        // Authorize Dedicated Hosts and Tournament Moderators
        var authorizationResult = await _authorizationService.AuthorizeAsync(User);
        if (!authorizationResult.Succeeded)
        {
            return Forbid(); 
        }*/
            
        if (matchId is null)
        {
            return TypedResults.NotFound();
        }
        
        var match = await dbContext
            .Matches
            .FirstOrDefaultAsync(x => x.Id == matchId, token);


        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.Execute(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

            match.Complete(winnerId);
            var msg = new MatchCompleted {
                MatchId = matchId,
                TournamentId = match.TournamentId,
                WinnerId = match.WinnerId,
                CompletedLocalMatchId = match.LocalMatchId
            };

            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:match-completed"));
            await endpoint.Send(msg);
        });

        return TypedResults.Ok();
    }
}