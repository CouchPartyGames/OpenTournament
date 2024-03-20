using System.Text.Json;

namespace OpenTournament.Data.Models;

public record OutboxId(Guid Id)
{
   public static OutboxId NewOutboxId() => new OutboxId(Guid.NewGuid());
}

public sealed class Outbox
{
   public OutboxId Id { get; private set; }

   public string Data { get; private set; } = String.Empty;
   
   public DateTime Created { get; init; }
   
   public DateTime Processed { get; private set; }
   
   public Status Status { get; set; }

   /*
   public void Create(IDomainEvent event)
   {
      Id = OutboxId.NewOutboxId();
      Data = JsonSerializer.Serialize(event);
   }*/
   
   public void SetProcessed()
   {
      Processed = new DateTime();
      Status = Status.Completed;
   }
}