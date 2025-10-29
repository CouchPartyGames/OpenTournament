using ErrorOr;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Registration.List;

public static class ListRegistrationHandler
{

    public static async Task<ErrorOr<ListRegistrationResponse>> HandleAsync(string id, 
        AppDbContext dbContext, 
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }
        
        var participants = await dbContext
            .Registrations
            .Where(x => x.TournamentId == tournamentId)
            .Select(x => x.Participant)
            .ToListAsync(cancellationToken: token);
        
        return new ListRegistrationResponse(participants);
    }
}