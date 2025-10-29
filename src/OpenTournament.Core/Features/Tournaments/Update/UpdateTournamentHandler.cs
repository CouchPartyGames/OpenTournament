using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Update;

using ErrorOr;

public static class UpdateTournamentHandler
{
    public static async Task<ErrorOr<Updated>> HandleAsync(
        string id,
        UpdateTournamentCommand command,
        AppDbContext dbContext, 
        CancellationToken token)
    {

        Validator validator = new();
        ValidationResult validationResult = await validator.ValidateAsync(command, token);
        if (!validationResult.IsValid)
        {
            return Error.Validation();
            //return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var tournamentId = TournamentId.TryParse(id);
        var tournament = await dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
        if (tournament is null)
        {
            return Error.NotFound();
        }

        var engine = new RuleEngine();
        engine.Add(new TournamentInRegistrationState(tournament.Status));
        if (!engine.Evaluate())
        {
            return Error.Failure();
            //return TypedResults.ValidationProblem(engine.ToValidationExtensions());
        }

        tournament.Name = command.Name;
        await dbContext.SaveChangesAsync(token);
      
        return Result.Updated;
    }
}