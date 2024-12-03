using MassTransit;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Features.Tournaments.Create;


public static class CreateTournamentEndpoint
{

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