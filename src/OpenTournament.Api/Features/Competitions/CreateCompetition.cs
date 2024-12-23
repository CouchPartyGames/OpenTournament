using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Features.Competitions;

public static class CreateCompetition
{
    public sealed record CreateCompetitionCommand(
        [Required]
        [property: Description("name of the competition")]
        string Name);

    public static async Task<Results<Created, ValidationProblem>> Endpoint(AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var competition = new Competition
        {
            CompetitionId = CompetitionId.New(),
            Name = "My Name"
        };
        dbContext.Add(competition);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return TypedResults.Created();
    }
}