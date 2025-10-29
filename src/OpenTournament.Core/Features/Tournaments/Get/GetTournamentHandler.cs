using CouchPartyGames.TournamentGenerator;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Get;

using ErrorOr;

public static class GetTournamentHandler
{
    public static async Task<ErrorOr<GetTournamentResponse>> HandleAsync(string id, AppDbContext dbContext, CancellationToken token)
    {
        
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }

        var tournament = await dbContext
                .Tournaments
                .Include(m => m.Matches)
                .FirstOrDefaultAsync(m => m.Id == tournamentId, token);

        return new GetTournamentResponse(tournament);
    }
    
}