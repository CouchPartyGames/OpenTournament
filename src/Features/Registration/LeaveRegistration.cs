using OpenTournament.Common;
using OpenTournament.Common.Models;

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
                                          && r.TournamentId == command.TournamentId);
            
            if (registration is null)
            {
                return new OneOf.Types.NotFound();
            }

            _dbContext.Remove(registration);
            var result = await _dbContext.SaveChangesAsync(token);
            
            return true;
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("registrations/{id}/leave", Endpoint)
            .WithTags("Registration");

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {

        var tournGuid = Guid.NewGuid();
        var partGuid = Guid.NewGuid();
        
        var command = new LeaveTournamentCommand(new TournamentId(tournGuid),
            new ParticipantId(partGuid));
        var result = await mediator.Send(command, token);
        return result.Match<Results<NoContent, NotFound>>(
            _ => TypedResults.NoContent(),
            _ => TypedResults.NotFound());
    }
}