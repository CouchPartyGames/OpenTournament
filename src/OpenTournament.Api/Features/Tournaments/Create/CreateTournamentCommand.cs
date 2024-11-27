namespace OpenTournament.Features.Tournaments.Create;

using OpenTournament.Data.Models;

public sealed record CreateTournamentCommand(string Name,
	DateTime StartTime) : IRequest<OneOf<TournamentId, ValidationFailure, ProblemDetails>>;