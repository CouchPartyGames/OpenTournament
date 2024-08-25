using FirebaseAdmin.Auth;
using OneOf.Types;
using OpenTournament.Authentication;
using OpenTournament.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Features.Authentication;

public static class Login
{

    public sealed record AuthCommand(ParticipantId Id) : IRequest<bool>;

    internal sealed class Handler : IRequestHandler<AuthCommand, bool>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<bool> Handle(AuthCommand request, CancellationToken cancellationToken)
        {
            var participant = await _dbContext
                .Participants
                .FirstOrDefaultAsync(p => p.Id == request.Id);
            
            if (participant is null)
            {
                
                var newParticipant = new Participant()
                {
                    Id = request.Id,
                    Name = "hello",
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
        HttpContext httpContext,
        CancellationToken token)
    {
        var userId = httpContext.GetUserId();
        var command = new AuthCommand(new ParticipantId(userId));
        var result = await mediator.Send(command, token);
        return TypedResults.NoContent();
    }
}