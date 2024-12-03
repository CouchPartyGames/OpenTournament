using MassTransit;
using OpenTournament.Api.Authentication;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;
using OpenTournament.Api.Features.Tournaments.Create;

namespace OpenTournament.Api.Features.Tournaments;

public static class CreateTournament
{
    
	public static async Task<Results<Created, ProblemHttpResult>> EndPoint(CreateTournamentCommand request, 
		IMediator mediator, 
		HttpContext httpContext,
		IPublishEndpoint publishEndpoint,
		AppDbContext dbContext,
		CancellationToken token)
	{
		var creatorId = httpContext.GetUserId();
		var tournament = new Tournament
		{
			Id = TournamentId.NewTournamentId(),
			Name = request.Name,
			Creator = Creator.New(new ParticipantId(creatorId))
		};

		dbContext.Add(tournament);
		await dbContext.SaveChangesAsync(token);
		await publishEndpoint.Publish(TournamentCreated.New(tournament), token);

		return TypedResults.Created(tournament.Id.Value.ToString());
	}
}