namespace OpenTournament.Features.Tournaments.Create;

public static class CreateTournament
{
	public static void MapEndpoint(this IEndpointRouteBuilder app) =>
		app.MapPost("tournaments", EndPoint)
			.WithTags("Tournament")
			.WithSummary("Create Tournament")
			.WithDescription("Create a new tournament")
			.WithOpenApi();
//			.RequireAuthorization();


	public static async Task<Results<Created, ProblemHttpResult>> EndPoint(CreateTournamentCommand request, 
		IMediator mediator, 
		HttpContext httpContext,
		CancellationToken token)
	{
		var result = await mediator.Send(request, token);
		return result.Match<Results<Created, ProblemHttpResult>>(
			tournamentId => TypedResults.Created(tournamentId.Value.ToString()),
			validateError => TypedResults.Problem(detail:"validation error"),
			internalError => TypedResults.Problem(internalError));
	}
    
}