using MassTransit;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Jobs;

public sealed class RegistrationFinalizedConsumer(ILogger<RegistrationFinalizedConsumer> logger) : IConsumer<RegistrationFinalized>
{
    public Task Consume(ConsumeContext<RegistrationFinalized> context)
    {
        logger.LogInformation("Registration Finalized");

        // Grab Registrations
        throw new NotImplementedException();
    }
}