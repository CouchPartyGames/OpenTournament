using OpenTournament.Common;

namespace Features.Tournaments;

public static class UpdateTournament
{
   public sealed record UpdateTournamentCommand(string Id, string Name) : IRequest<OneOf<bool, OneOf.Types.NotFound, ProblemDetails>>;
   
   internal sealed class Handler : IRequestHandler<UpdateTournamentCommand, OneOf<bool, OneOf.Types.NotFound, ProblemDetails>>
   {
      private readonly AppDbContext _dbContext;
      
      public Handler(AppDbContext dbContext)
      {
         _dbContext = dbContext;
      }

		
      public async ValueTask<OneOf<bool, OneOf.Types.NotFound, ProblemDetails>> Handle(UpdateTournamentCommand request, 
         CancellationToken cancellationToken)
      {
         Validator validator = new();
         ValidationResult result = validator.Validate(request);
         if (!result.IsValid)
         {
            //result.Errors;
            Console.WriteLine("Validation Failure");
            //return new ValidationFailure();
         }

         
         var tournament = await _dbContext.Tournaments.FirstOrDefaultAsync(m => m.Id == new Guid(request.Id));
         if (tournament is null)
         {
            return new OneOf.Types.NotFound();
         }

         tournament.Name = request.Name;
         
         var results = await _dbContext.SaveChangesAsync(cancellationToken);
         if (results < 1)
         {
            Console.WriteLine($"Database Failure {results}");
            return new ProblemDetails
            {
               Title = "Internal Server Error",
               Status = 500
            };
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

   public static void MapEndpoint(this IEndpointRouteBuilder app)
   {
      app.MapPut("tournaments/{id}", async (string id, 
         UpdateTournamentCommand request, 
         IMediator mediator,
         CancellationToken token) =>
      {
         var commandRequest = request with { Id = id };
         var result = await mediator.Send(commandRequest, token);
         return result.Match<Results<NoContent, NotFound, ProblemHttpResult>>(
            sucessful => TypedResults.NoContent(),
            _ => TypedResults.NotFound(),
            internalError => TypedResults.Problem());
      });
   }
}
