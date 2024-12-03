using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Features.Tournaments.Create;

public sealed record CreateTournamentCommand(string Name,
	DateTime StartTime) : IRequest<OneOf<TournamentId, ValidationFailure, ProblemDetails>>;