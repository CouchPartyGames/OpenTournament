using MassTransit;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;
using OpenTournament.Api.Identity.Authentication;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace OpenTournament.Api.Features.Tournaments;

public static class CreateTournament
{
	public sealed record CreateTournamentCommand(string Name,
		DateTime StartTime,
		int MinParticipants,
		int MaxParticipants,
		EliminationMode Mode,
		DrawSeeding Seeding);
	
	private sealed class CreateTournamentValidator : AbstractValidator<CreateTournamentCommand>
	{
		public CreateTournamentValidator()
		{
			RuleFor(c => c.Name)
				.Length(3, 50)
				.NotEmpty();
			
			RuleFor(c => c.StartTime)
				.GreaterThan(DateTime.Now);

			RuleFor(c => c.MinParticipants)
				.GreaterThan(2);

			RuleFor(c => c.MaxParticipants)
				.GreaterThan(c => c.MinParticipants)
				.LessThanOrEqualTo(256);

			RuleFor(c => c.Mode)
				.IsInEnum();

			RuleFor(c => c.Seeding)
				.IsInEnum();

		}
	}
    
	public static async Task<Results<Created, ValidationProblem, ProblemHttpResult>> EndPoint(CreateTournamentCommand command, 
		IMediator mediator, 
		HttpContext httpContext,
		IPublishEndpoint publishEndpoint,
		AppDbContext dbContext,
		CancellationToken token)
	{ 
		CreateTournamentValidator validator = new(); 
		ValidationResult validationResult = await validator.ValidateAsync(command, token); 
		if (!validationResult.IsValid) 
		{ 
			return TypedResults.ValidationProblem(validationResult.ToDictionary()); 
		}
		
		var creatorId = httpContext.GetUserId();
		var tournament = new Tournament
		{
			Id = TournamentId.NewTournamentId(),
			Name = command.Name,
			Creator = Creator.New(new ParticipantId(creatorId))
		};

		dbContext.Add(tournament);
		await dbContext.SaveChangesAsync(token);
		await publishEndpoint.Publish(TournamentCreated.New(tournament), token);

		return TypedResults.Created(tournament.Id.Value.ToString());
	}
}