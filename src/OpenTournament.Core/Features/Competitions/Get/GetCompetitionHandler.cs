using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Competitions.Get;

using ErrorOr;

public static class GetCompetitionHandler
{
    public async static Task<ErrorOr<GetCompetitionResponse>> HandleAsync(GetCompetitionQuery query, AppDbContext dbContext, CancellationToken ct)
    {
        return new GetCompetitionResponse();
    }
}