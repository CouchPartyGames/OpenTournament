using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;
using EventId = OpenTournament.Api.Data.Models.EventId;

namespace OpenTournament.Api.Features.Events;

public static class CreateEvent
{
    public static async Task<Results<Created, ValidationProblem>> Endpoint(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {

        var myEvent = new Event()
        {
            EventId = EventId.New(),
            Name = "New Event",
            Description = "New Event",
            Slug = "new-event"
        };
        dbContext.Add(myEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created();
    }
}