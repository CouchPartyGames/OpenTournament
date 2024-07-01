namespace OpenTournament.Features.Tournaments.Create;

public class CreateTournamentValidator : AbstractValidator<CreateTournamentCommand>
{
	public CreateTournamentValidator()
	{
		RuleFor(c => c.Name)
			.NotEmpty()
			.MinimumLength(3);
	}
}