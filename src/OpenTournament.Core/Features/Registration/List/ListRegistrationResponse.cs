using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core.Features.Registration.List;

    
public sealed record ListRegistrationResponse(List<Participant> Registrations);