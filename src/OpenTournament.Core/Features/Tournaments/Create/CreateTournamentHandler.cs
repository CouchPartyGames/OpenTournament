using ErrorOr;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Create;

public static class CreateTournamentHandler
{
    public static async Task<ErrorOr<Created>> HandleAsync(CreateTournamentCommand command, 
        AppDbContext dbContext,
        CancellationToken ct)
    {
        CreateTournamentValidator validator = new(); 
        ValidationResult validationResult = await validator.ValidateAsync(command, ct); 
        if (!validationResult.IsValid)
        {
            return Error.Validation();
            //return TypedResults.ValidationProblem(validationResult.ToDictionary()); 
        }
		
        var creatorId = httpContext.GetUserId();
        var tournament = new Tournament
        {
            Id = TournamentId.NewTournamentId(),
            Name = command.Name,
            Creator = Creator.New(new ParticipantId(creatorId))
        };

        await dbContext.AddAsync(tournament, ct);
        await dbContext.SaveChangesAsync(ct);
        //await publishEndpoint.Publish(TournamentCreated.New(tournament), token);

        return Result.Created;
    }
}