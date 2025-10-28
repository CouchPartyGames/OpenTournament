using ErrorOr;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Competitions.Create;

public static class CreateCompetitionHandler
{
    public static async Task<ErrorOr<Created>> HandleAsync(CreateCompetitionCommand command, 
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var competition = new Competition
        {
            CompetitionId = CompetitionId.New(),
            Name = command.Name
        };
        await dbContext.AddAsync(competition, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created;
    }
}