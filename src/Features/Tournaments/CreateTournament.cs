using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Common;

namespace Features.Tournaments;

public static class CreateTournament
{
	public sealed record CreateTournamentCommand(string Name) : IRequest<OneOf<Guid, ValidationFailure, ProblemDetails>>;

	
	internal sealed class Handler : IRequestHandler<CreateTournamentCommand, OneOf<Guid, ValidationFailure, ProblemDetails>>
	{
		private readonly DbContext _dbContext;
		
		public Handler(AppDbContext dbContext) => _dbContext = dbContext;
		
		public async ValueTask<OneOf<Guid, ValidationFailure, ProblemDetails>> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
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

	public static void MapEndpoint(this IEndpointRouteBuilder app) => 
		app.MapPost("tournaments", CreateTournament.EndPoint);
	
	
	public static async Task<Results<Created, ProblemHttpResult>> EndPoint(CreateTournamentCommand request, 
		IMediator mediator, 
		CancellationToken token)
	{
		var result = await mediator.Send(request, token);
		return result.Match<Results<Created, ProblemHttpResult>>(
			created => TypedResults.Created(created.ToString()),
			validateError => TypedResults.Problem(detail:"validation error"),
			internalError => TypedResults.Problem(internalError));
	}
}
