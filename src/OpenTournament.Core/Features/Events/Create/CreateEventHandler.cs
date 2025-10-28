using ErrorOr;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Events.Create;

public static class CreateEventHandler
{
   public static async Task<ErrorOr<Created>> HandleAsync(CreateEventCommand command, 
      AppDbContext dbContext, 
      CancellationToken ct)
   {
      var myEvent = new Event()
      {
         EventId = EventId.New(),
         Name = command.Name,
         Description = "New Event",
         Slug = "new-event"
      };
      await dbContext.AddAsync(myEvent, ct);
      await dbContext.SaveChangesAsync(ct);

      return Result.Created;
      //return TypedResults.Created(myEvent.EventId.Value.ToString());
   } 
}