using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Competitions;

public static class CreateCompetition
{
    public sealed record CreateCompetitionCommand(
        [Required]
        [property: Description("name of the competition")]
        string Name);

    public static async Task<Results<Created, ValidationProblem>> Endpoint(CreateCompetitionCommand command, 
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var competition = new Competition
        {
            CompetitionId = CompetitionId.New(),
            Name = command.Name
        };
        dbContext.Add(competition);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return TypedResults.Created();
    }
}