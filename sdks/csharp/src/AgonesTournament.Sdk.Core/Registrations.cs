namespace AgonesTournament.Sdk.Core;

/// <summary>An external account identifying a Participant, such as Keycloak or Steam.</summary>
public sealed record PlayerIdentity
{
    /// <summary>The Player Identity kind, e.g. keycloak or steam.</summary>
    public required string Kind { get; init; }
    /// <summary>The identity's value, e.g. a Steam ID.</summary>
    public required string Value { get; init; }
}

/// <summary>A Participant registered in a Tournament, with their Player Identity and Check-in state.</summary>
public sealed record RegisteredParticipant
{
    /// <summary>When this Participant checked in; absent until Check-in.</summary>
    public DateTimeOffset? CheckedInAt { get; init; }
    /// <summary>The UUID of this domain object.</summary>
    public required ParticipantId Id { get; init; }
    /// <summary>The Player Identity identifying this Participant.</summary>
    public required PlayerIdentity Identity { get; init; }
    /// <summary>When the Participant entered the Tournament.</summary>
    public required DateTimeOffset RegisteredAt { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required ParticipantStatus Status { get; init; }
}

/// <summary>Participants belonging to the caller in one Tournament; the API may return a null collection.</summary>
public sealed record ParticipantRegistrations
{
    /// <summary>The Participants in this response; the API may return null.</summary>
    public required IReadOnlyList<RegisteredParticipant>? Participants { get; init; }
}
