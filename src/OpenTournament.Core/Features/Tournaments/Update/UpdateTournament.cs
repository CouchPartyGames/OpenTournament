using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Core.Features.Tournaments.Update;

public static class UpdateTournament
{
   

   private sealed class Validator : AbstractValidator<UpdateTournamentCommand>
   {
      public Validator()
      {
			RuleFor(c => c.Name)
				.Length(3, 50)
				.NotEmpty();
         
			RuleFor(c => c.StartTime)
				.GreaterThan(DateTime.Now);
      }
   }


   public static async Task<Results<NoContent, NotFound, ProblemHttpResult, ValidationProblem>> Endpoint(string id,
      UpdateTournamentCommand command,
      AppDbContext dbContext,
      CancellationToken token)
   {
      Validator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(command, token);
      if (!validationResult.IsValid)
      {
         return TypedResults.ValidationProblem(validationResult.ToDictionary());
      }

      var tournamentId = TournamentId.TryParse(id);
      var tournament = await dbContext
         .Tournaments
         .FirstOrDefaultAsync(m => m.Id == tournamentId, token);
      if (tournament is null)
      {
         return TypedResults.NotFound();
      }

      var engine = new RuleEngine();
      engine.Add(new TournamentInRegistrationState(tournament.Status));
      if (!engine.Evaluate())
      {
         return TypedResults.ValidationProblem(engine.ToValidationExtensions());
      }

      tournament.Name = command.Name;
      await dbContext.SaveChangesAsync(token);
      
      return TypedResults.NoContent();
   }
}
