using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Common;

namespace Features.Tournaments;

public static class CreateTournament
{
	public sealed record CreateTournamentCommand(string Name) : IRequest<bool>;

	public class Validator : AbstractValidator<CreateTournamentCommand>
	{
		public Validator()
		{
			RuleFor(c => c.Name)
				.NotEmpty()
				.MinimumLength(3);
		}
	}
	internal sealed class Handler : IRequestHandler<CreateTournamentCommand, bool>
	{
		private readonly DbContext _dbContext;
		
		public Handler(AppDbContext dbContext) => _dbContext = dbContext;
		
		public async ValueTask<bool> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
		{
			Validator validator = new();
			ValidationResult result = validator.Validate(request);
			if (!result.IsValid)
			{
				return false;
			}
			
			var tourny = new Tournament
			{
				Name = request.Name
			};
			Console.WriteLine($"Tournament: {tourny}");

			_dbContext.Add(tourny);
			await _dbContext.SaveChangesAsync(cancellationToken);

			return true;
		}
	}

	public static void MapEndpoint(this IEndpointRouteBuilder app) => 
		app.MapPost("tournaments", CreateTournament.EndPoint);
	
	
	public static async Task<Results<Created, BadRequest>> EndPoint(CreateTournamentCommand request, 
		IMediator mediator, 
		CancellationToken token)
	{
		var result = await mediator.Send(request, token);
		return TypedResults.Created();
	}
}
