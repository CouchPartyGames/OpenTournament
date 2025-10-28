using System.ComponentModel;

namespace OpenTournament.Core.Features.Events.Create;

public sealed record CreateEventCommand(
    [property: Description("name of the event")]
    string Name);
