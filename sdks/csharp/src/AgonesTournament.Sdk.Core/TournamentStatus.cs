using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The lifecycle state of a Tournament.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TournamentStatus>))]
public enum TournamentStatus
{
    /// <summary>Configuration is editable.</summary>
    [JsonStringEnumMemberName("draft")] Draft,
    /// <summary>The Registration Window is open.</summary>
    [JsonStringEnumMemberName("registration-open")] RegistrationOpen,
    /// <summary>The Check-in Window is open.</summary>
    [JsonStringEnumMemberName("check-in")] CheckIn,
    /// <summary>The Tournament is playing.</summary>
    [JsonStringEnumMemberName("running")] Running,
    /// <summary>The Tournament has finished.</summary>
    [JsonStringEnumMemberName("completed")] Completed,
    /// <summary>The Tournament has been cancelled.</summary>
    [JsonStringEnumMemberName("cancelled")] Cancelled
}

/// <summary>The status of a Participant.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParticipantStatus>))]
public enum ParticipantStatus
{
    /// <summary>Registered.</summary>
    [JsonStringEnumMemberName("registered")] Registered,
    /// <summary>Checked in.</summary>
    [JsonStringEnumMemberName("checked-in")] CheckedIn,
    /// <summary>Not checked in.</summary>
    [JsonStringEnumMemberName("not-checked-in")] NotCheckedIn,
    /// <summary>Active.</summary>
    [JsonStringEnumMemberName("active")] Active,
    /// <summary>Withdrawn.</summary>
    [JsonStringEnumMemberName("withdrawn")] Withdrawn,
    /// <summary>Disqualified.</summary>
    [JsonStringEnumMemberName("disqualified")] Disqualified,
    /// <summary>Eliminated.</summary>
    [JsonStringEnumMemberName("eliminated")] Eliminated,
}

/// <summary>The status of a ConfiguredStage.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StageStatus>))]
public enum StageStatus
{
    /// <summary>Pending.</summary>
    [JsonStringEnumMemberName("pending")] Pending,
    /// <summary>Running.</summary>
    [JsonStringEnumMemberName("running")] Running,
    /// <summary>Completed.</summary>
    [JsonStringEnumMemberName("completed")] Completed,
}

/// <summary>The bracket of a Round.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Bracket>))]
public enum Bracket
{
    /// <summary>Upper.</summary>
    [JsonStringEnumMemberName("upper")] Upper,
    /// <summary>Lower.</summary>
    [JsonStringEnumMemberName("lower")] Lower,
    /// <summary>Grand final.</summary>
    [JsonStringEnumMemberName("grand-final")] GrandFinal,
}

/// <summary>The result of a Match.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchResult>))]
public enum MatchResult
{
    /// <summary>Win.</summary>
    [JsonStringEnumMemberName("win")] Win,
    /// <summary>Double forfeit.</summary>
    [JsonStringEnumMemberName("double-forfeit")] DoubleForfeit,
    /// <summary>Bye.</summary>
    [JsonStringEnumMemberName("bye")] Bye,
    /// <summary>Empty.</summary>
    [JsonStringEnumMemberName("empty")] Empty,
    /// <summary>Free for all.</summary>
    [JsonStringEnumMemberName("free-for-all")] FreeForAll,
}

