using OpenTournament.Api.Authentication;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Api.Features.Authentication;

public static class Login
{

    
    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", Endpoint)
            .WithTags("Auth")
            .WithSummary("Login")
            .WithDescription("Login/Register a user")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<NoContent, ForbidHttpResult, Conflict, NotFound>> Endpoint(IMediator mediator, 
        HttpContext httpContext,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var userId = httpContext.GetUserId();
        if (userId is null)
        {
            return TypedResults.Forbid();
        }

        ParticipantId participantId = new ParticipantId(userId);
        var participant = await dbContext
            .Participants
            .FirstOrDefaultAsync(p => p.Id == participantId, token);
        if (participant is not null)
        {
            return TypedResults.Conflict();
        } 
            
        var newParticipant = new Participant()
        {
            Id = participantId,
            Name = "hello",
            Rank = 1
        };
        dbContext.Add(newParticipant);
        await dbContext.SaveChangesAsync(token);
        
        return TypedResults.NoContent();
    }
}