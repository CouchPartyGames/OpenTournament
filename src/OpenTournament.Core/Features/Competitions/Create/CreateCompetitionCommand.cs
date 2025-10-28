using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenTournament.Core.Features.Competitions.Create;

public sealed record CreateCompetitionCommand(
    [Required]
    [property: Description("name of the competition")]
    string Name);
