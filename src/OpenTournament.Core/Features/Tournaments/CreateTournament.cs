using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MassTransit;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;
using OpenTournament.Api.Identity.Authentication;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace OpenTournament.Api.Features.Tournaments;

public static class CreateTournament
{
	public sealed record CreateTournamentCommand(
		[Required]
		[property: Description("name of the tournament")]	
		string Name,
		[property: Description("start time of the tournament")]
		DateTime StartTime,
		[property: Description("minimum number of users required to start a tournament")]
		int MinParticipants,
		[property: Description("maximum number of users allowed to join")]
		int MaxParticipants,
		[property: Description("single or double elimination tournament")]
		EliminationMode Mode,
		[property: Description("seed players randomly or by rank")]
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