using System.ComponentModel;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence;
using EventId = OpenTournament.Api.Data.Models.EventId;

namespace OpenTournament.Core.Features.Events;

public static class CreateEvent
{
    public sealed record CreateEventCommand(
        [property: Description("name of the event")]
        string Name);
    
    public static async Task<Results<Created, ValidationProblem>> Endpoint(CreateEventCommand command,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {

        var myEvent = new Event()
        {
            EventId = EventId.New(),
            Name = command.Name,
            Description = "New Event",
            Slug = "new-event"
        };
        dbContext.Add(myEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(myEvent.EventId.Value.ToString());
    }
}