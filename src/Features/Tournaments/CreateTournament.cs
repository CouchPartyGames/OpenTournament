using OpenTournament.Models;

namespace Features.Tournaments;

public static class CreateTournament
{
	public sealed record CreateTournamentCommand(string Name,
		DateTime StartTime) : IRequest<OneOf<TournamentId, ValidationFailure, ProblemDetails>>;

	
	internal sealed class Handler : IRequestHandler<CreateTournamentCommand, OneOf<TournamentId, ValidationFailure, ProblemDetails>>
	{
		private readonly AppDbContext _dbContext;
		
		public Handler(AppDbContext dbContext) => _dbContext = dbContext;
		
		public async ValueTask<OneOf<TournamentId, ValidationFailure, ProblemDetails>> Handle(CreateTournamentCommand request, 
			CancellationToken cancellationToken)
		{
			Validator validator = new();
			ValidationResult result = validator.Validate(request);
			if (!result.IsValid)
			{
				//result.Errors;
				Console.WriteLine("Validation Failure");
				return new ValidationFailure();
			}
			
			var tourny = new Tournament
			{
				Id = TournamentId.Create(),
				Name = request.Name
			};

			_dbContext.Add(tourny);
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

			return tourny.Id;
		}
	}
	
	public class Validator : AbstractValidator<CreateTournamentCommand>
	{
		public Validator()
		{
			RuleFor(c => c.Name)
				.NotEmpty()
				.MinimumLength(3);
		}
	}

	// <summary>
	// Create a Tournament
	// </summary>
	public static void MapEndpoint(this IEndpointRouteBuilder app) =>
		app.MapPost("tournaments", EndPoint)
			.WithTags("Tournament")
			.WithDescription("Create a Tournament")
			.WithOpenApi()
			.RequireAuthorization();
	
	
	public static async Task<Results<Created, ProblemHttpResult>> EndPoint(CreateTournamentCommand request, 
		IMediator mediator, 
		CancellationToken token)
	{
		var result = await mediator.Send(request, token);
		return result.Match<Results<Created, ProblemHttpResult>>(
			tournamentId => TypedResults.Created(tournamentId.Value.ToString()),
			validateError => TypedResults.Problem(detail:"validation error"),
			internalError => TypedResults.Problem(internalError));
	}
}
