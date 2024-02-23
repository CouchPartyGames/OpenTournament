using FirebaseAdmin.Auth;
using OneOf.Types;
using OpenTournament.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Features.Authentication;

public static class Login
{

    public sealed record AuthCommand() : IRequest<bool>;

    internal sealed class Handler : IRequestHandler<AuthCommand, bool>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<bool> Handle(AuthCommand request, CancellationToken cancellationToken)
        {
            var userId = new ParticipantId("aldfadsf");
            var name = "hello";
            
            var participant = await _dbContext
                .Participants
                .FirstOrDefaultAsync(p => p.Id == userId);
            
            if (participant is null)
            {
                
                var newParticipant = new Participant()
                {
                    Id = userId,
                    Name = name,
                    Rank = 1
                };
                _dbContext.Add(newParticipant);
            }

            return true;
        }
    }
    

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", Endpoint)
            .WithTags("Auth")
            .WithSummary("Login")
            .WithDescription("Login/Register a user")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<NoContent, NotFound>> Endpoint(IMediator mediator, 
        CancellationToken token)
    {
        var command = new AuthCommand();
        var result = await mediator.Send(command, token);
        return TypedResults.NoContent();
    }
}