namespace OpenTournament.Features.Tournaments.Create;

public class Validator : AbstractValidator<CreateTournamentCommand>
{
	public Validator()
	{
		RuleFor(c => c.Name)
			.NotEmpty()
			.MinimumLength(3);
	}
}