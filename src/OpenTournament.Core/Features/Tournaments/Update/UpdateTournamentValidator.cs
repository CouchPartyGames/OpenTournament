using FluentValidation;

namespace OpenTournament.Core.Features.Tournaments.Update;

public class UpdateTournamentValidator : AbstractValidator<UpdateTournamentCommand>
{
    public UpdateTournamentValidator()
    {
        RuleFor(c => c.Name)
            .Length(3, 50)
            .NotEmpty();
     
        RuleFor(c => c.StartTime)
            .GreaterThan(DateTime.Now);
    }
}