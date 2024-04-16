using System.Diagnostics.Metrics;

namespace OpenTournament.Common;

public sealed class OpenTournamentMetrics
{
   private readonly Counter<int> _outboxMessages;
   
   public OpenTournamentMetrics(IMeterFactory factory)
   {
      var meter = factory.Create("OpenTournament");
      _outboxMessages = meter.CreateCounter<int>("opentournament.outbox.count");
   }

   public void AddOutboxMessages(int number = 1) => _outboxMessages.Add(number);
}