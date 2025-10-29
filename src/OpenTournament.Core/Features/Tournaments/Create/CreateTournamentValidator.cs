using FluentValidation;

namespace OpenTournament.Core.Features.Tournaments.Create;

public sealed class CreateTournamentValidator : AbstractValidator<CreateTournamentCommand>
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
