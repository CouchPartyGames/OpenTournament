using OpenTournament.Data.DomainEvents;
using OpenTournament.Data.Models;

namespace Features.Tournaments;

public static class LeaveRegistration
{
    public sealed record LeaveTournamentCommand(TournamentId TournamentId, ParticipantId ParticiantId) :
        IRequest<OneOf<bool, OneOf.Types.NotFound>>;

    internal sealed class Handler : IRequestHandler<LeaveTournamentCommand, OneOf<bool, OneOf.Types.NotFound>>
    {
        private readonly AppDbContext _dbContext;
        
        public Handler(AppDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<OneOf<bool, OneOf.Types.NotFound>> Handle(LeaveTournamentCommand command,
            CancellationToken token)
        {
            var registration = await _dbContext
                .Registrations
                .FirstOrDefaultAsync(r => r.ParticipantId == command.ParticiantId 
                                          && r.TournamentId == command.TournamentId, token);
            
            if (registration is null)
            {
                return new OneOf.Types.NotFound();
            }

            //new LeftTournamentEvent(command.TournamentId, command.ParticiantId);
            _dbContext.Remove(registration);
            var result = await _dbContext.SaveChangesAsync(token);
            
            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("registrations/{id}/leave", Endpoint)
            .WithTags("Registration")
            .WithSummary("Leave Tournament")
            .WithDescription("Leave Tournament")
            .WithOpenApi()
            .RequireAuthorization();

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id,
        IMediator mediator,
        HttpContext context,
        CancellationToken token)
    {
        var participantId = context.User.Claims.FirstOrDefault(c => c.Type == "user_id");
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return TypedResults.NotFound();
        }
        
        var command = new LeaveTournamentCommand(tournamentId,
            new ParticipantId(participantId.Value));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound>>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound());
    }
}