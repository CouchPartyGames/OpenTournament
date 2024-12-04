using OpenTournament.Api.Common.Rules;
using OpenTournament.Api.Common.Rules.Tournaments;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Api.Features.Tournaments;

public static class UpdateTournament
{
   public sealed record UpdateTournamentCommand(TournamentId Id, string Name) : IRequest<OneOf<bool, OneOf.Types.NotFound>>;
   

   private sealed class Validator : AbstractValidator<UpdateTournamentCommand>
   {
      public Validator()
      {
         RuleFor(c => c.Name)
            .NotEmpty()
            .MinimumLength(3);
      }
   }


   public static async Task<Results<NoContent, NotFound, ProblemHttpResult, ValidationProblem>> Endpoint(string id,
      UpdateTournamentCommand request,
      IMediator mediator,
      AppDbContext dbContext,
      CancellationToken token)
   {
      var tournamentId = TournamentId.TryParse(id);
      if (tournamentId is null)
      {
         return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
      }
         
      var command = request with { Id = tournamentId };
      Validator validator = new();
      ValidationResult validationResult = validator.Validate(command);
      if (!validationResult.IsValid)
      {
         return TypedResults.ValidationProblem(validationResult.ToDictionary());
      }


      var tournament = await dbContext
         .Tournaments
         .FirstOrDefaultAsync(m => m.Id == command.Id, token);
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
