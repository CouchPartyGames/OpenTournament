using OpenTournament.Common;
using OpenTournament.Common.Models;

namespace Features.Tournaments;

public static class UpdateTournament
{
   public sealed record UpdateTournamentCommand(TournamentId Id, string Name) : IRequest<OneOf<bool, OneOf.Types.NotFound>>;
   
   internal sealed class Handler : IRequestHandler<UpdateTournamentCommand, OneOf<bool, OneOf.Types.NotFound>>
   {
      private readonly AppDbContext _dbContext;
      
      public Handler(AppDbContext dbContext)
      {
         _dbContext = dbContext;
      }

		
      public async ValueTask<OneOf<bool, OneOf.Types.NotFound>> Handle(UpdateTournamentCommand command, 
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


         //command.Id;
         var tournament = await _dbContext
            .Tournaments
            .FirstOrDefaultAsync(m => m.Id == command.Id);
         
         if (tournament is null)
         {
            return new OneOf.Types.NotFound();
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

   public static void MapEndpoint(this IEndpointRouteBuilder app)
   {
      app.MapPut("tournaments/{id}", async (string id, 
         UpdateTournamentCommand request, 
         IMediator mediator,
         CancellationToken token) => 
         {
            return UpdateTournament.Endpoint(id, request, mediator, token); 
         })
         .WithTags("Tournament")
         .WithDescription("Update a Tournament");
   }

   public static async Task<Results<NoContent, NotFound>> Endpoint(string id,
      UpdateTournamentCommand request,
      IMediator mediator,
      CancellationToken token)
   {
      var tournamentId = TournamentId.TryParse(id);
      if (tournamentId is null)
      {
         return TypedResults.NotFound();
      }
         
      var commandRequest = request with { Id = tournamentId };
      var result = await mediator.Send(commandRequest, token);
      return result.Match<Results<NoContent, NotFound>>(
         sucessful => TypedResults.NoContent(),
         _ => TypedResults.NotFound()); 
   }
}
