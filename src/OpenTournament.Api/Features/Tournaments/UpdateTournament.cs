using OpenTournament.Common.Rules;
using OpenTournament.Common.Rules.Tournaments;
using OpenTournament.Data.Models;
using NotFound = Microsoft.AspNetCore.Http.HttpResults.NotFound;

namespace OpenTournament.Features.Tournaments;

public static class UpdateTournament
{
   public sealed record UpdateTournamentCommand(TournamentId Id, string Name) : IRequest<OneOf<bool, OneOf.Types.NotFound, RuleFailure>>;
   
   internal sealed class Handler : IRequestHandler<UpdateTournamentCommand, OneOf<bool, OneOf.Types.NotFound, RuleFailure>>
   {
      private readonly AppDbContext _dbContext;
      
      public Handler(AppDbContext dbContext)
      {
         _dbContext = dbContext;
      }

		
      public async ValueTask<OneOf<bool, OneOf.Types.NotFound, RuleFailure>> Handle(UpdateTournamentCommand command, 
         CancellationToken cancellationToken)
      {
         Validator validator = new();
         ValidationResult result = validator.Validate(command);
         if (!result.IsValid)
         {
            //result.Errors;
            Console.WriteLine("Validation Failure");
            //return new ValidationFailure();
         }


         var tournament = await _dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken);
         
         if (tournament is null)
         {
            return new OneOf.Types.NotFound();
         }

         var engine = new RuleEngine();
         engine.Add(new TournamentInRegistrationState(tournament.Status));
         if (!engine.Evaluate())
         {
            return new RuleFailure(engine.Errors);
         }

         tournament.Name = command.Name;
         
         var results = await _dbContext.SaveChangesAsync(cancellationToken);
         if (results < 1)
         {
            /*j
            Console.WriteLine($"Database Failure {results}");
            return new ProblemDetails
            {
               Title = "Internal Server Error",
               Status = 500
            };*/
         }

         return true;
      }
   }
   
   public class Validator : AbstractValidator<UpdateTournamentCommand>
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
      CancellationToken token)
   {
      var tournamentId = TournamentId.TryParse(id);
      if (tournamentId is null)
      {
         return TypedResults.ValidationProblem(ValidationErrors.TournamentIdFailure);
      }
         
      var commandRequest = request with { Id = tournamentId };
      var result = await mediator.Send(commandRequest, token);
      return result.Match<Results<NoContent, NotFound, ProblemHttpResult, ValidationProblem>>(
         sucessful => TypedResults.NoContent(),
         _ => TypedResults.NotFound(),
         errors =>
         {
            return TypedResults.Problem(title: "Rule Failures",
               detail: errors.Errors[0].ToString(),
               statusCode: StatusCodes.Status400BadRequest);
         }); 
   }
}
