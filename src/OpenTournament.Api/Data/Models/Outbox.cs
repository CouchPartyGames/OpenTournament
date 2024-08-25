using System.Text.Json;

namespace OpenTournament.Data.Models;

public record OutboxId(Guid Value)
{
    public static OutboxId? TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new(guid);
    }
    
   public static OutboxId NewOutboxId() => new OutboxId(Guid.NewGuid());
}

public sealed class Outbox
{
   public enum Status
   {
      Ready = 0,
      Processed
   };
   
   public OutboxId Id { get; private set; }
   
   public string EventName { get; private set; }

   public string Content { get; private set; } = String.Empty;
   
   public DateTime Created { get; init; }
   
   public DateTime Processed { get; private set; }
   
   public Status State { get; set; }

   
   public static Outbox Create(IDomainEvent eventData) =>
      new Outbox()
      {
         Id = OutboxId.NewOutboxId(),
         EventName = nameof(eventData),
         Content = JsonSerializer.Serialize(eventData),
         Created = new DateTime(),
         State = Status.Ready
      };
   
   public void SetProcessed()
   {
      Processed = new DateTime();
      State = Status.Processed;
   }
}