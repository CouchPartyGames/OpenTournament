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
    /// <summary>CheckedInAt.</summary>
    public DateTimeOffset? CheckedInAt { get; init; }
    /// <summary>Id.</summary>
    public required ParticipantId Id { get; init; }
    /// <summary>Identity.</summary>
    public required PlayerIdentity Identity { get; init; }
    /// <summary>RegisteredAt.</summary>
    public required DateTimeOffset RegisteredAt { get; init; }
    /// <summary>Status.</summary>
    public required ParticipantStatus Status { get; init; }
}

/// <summary>Participants belonging to the caller in one Tournament; the API may return a null collection.</summary>
public sealed record ParticipantRegistrations
{
    /// <summary>Participants.</summary>
    public required IReadOnlyList<RegisteredParticipant>? Participants { get; init; }
}
