using MassTransit;
using OpenTournament.Data.DomainEvents;

namespace OpenTournament.Jobs;

public sealed class RegistrationFinalizedConsumer(ILogger<RegistrationFinalizedConsumer> logger) : IConsumer<RegistrationFinalized>
{
    public Task Consume(ConsumeContext<RegistrationFinalized> context)
    {
        logger.LogInformation("Registration Finalized");

        // Grab Registrations
        throw new NotImplementedException();
    }
}