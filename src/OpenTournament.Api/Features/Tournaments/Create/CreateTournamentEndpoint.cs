using OpenTournament.Data.Models;
using OpenTournament.Data.DomainEvents;
using MassTransit;

namespace OpenTournament.Features.Tournaments.Create;


public static class CreateTournamentEndpoint
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
		IPublishEndpoint publishEndpoint,
		AppDbContext dbContext,
		CancellationToken token)
	{
		var tourny = new Tournament
		{
			Id = TournamentId.NewTournamentId(),
			Name = request.Name
		};

		dbContext.Add(tourny);
		var results = await dbContext.SaveChangesAsync(token);
		if (results < 1)
		{
			return TypedResults.Problem(new ProblemDetails
			{
				Title = "Internal Server Error",
				Status = 500
			});
		}

		var msg = new TournamentCreated {
			TournamentId = tourny.Id,
			TournamentName = tourny.Name
		};
		await publishEndpoint.Publish(msg);

		return TypedResults.Created(tourny.Id.Value.ToString());
	}
    
}