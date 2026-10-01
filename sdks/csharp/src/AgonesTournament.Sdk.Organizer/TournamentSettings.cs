using System.Text.Json.Serialization;
using AgonesTournament.Sdk.Core;

namespace AgonesTournament.Sdk.Organizer;

/// <summary>Complete Draft settings, replaced on edit and frozen when registration opens. The API validates cross-field rules.</summary>
public record TournamentSettings
{
    /// <summary>The Organizer's name for this Tournament, 1–200 characters.</summary>
    public required string Name { get; init; }
    /// <summary>The future start time, after RegistrationOpensAt.</summary>
    public required DateTimeOffset StartsAt { get; init; }
    /// <summary>When the Registration Window opens; it closes at StartsAt.</summary>
    public required DateTimeOffset RegistrationOpensAt { get; init; }
    /// <summary>Maximum Participants, 2–100000.</summary>
    public required int Capacity { get; init; }
    /// <summary>Minimum Participants to start, at least 2 and at most Capacity.</summary>
    public required int MinimumParticipants { get; init; }
    /// <summary>Omit to disable Check-in. Enabled windows need at least 60 seconds and must fit inside the Registration Window.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CheckInSettings? CheckIn { get; init; }
    /// <summary>1–10 Stages in play order. Each Group needs at least two Participants and more than its Advancement.</summary>
    public required IReadOnlyList<StageSettings>? Stages { get; init; }
}

/// <summary>Creates a Tournament for a Game; all settings obey the same rules as Draft edits.</summary>
public sealed record NewTournament : TournamentSettings
{
    /// <summary>The nonempty Game catalog identifier; the Game cannot be changed after creation.</summary>
    public required string GameId { get; init; }
}

/// <summary>A Stage's Format and progression settings. The service validates combinations and the Game's Maximum Match Size.</summary>
public sealed record StageSettings
{
    /// <summary>The rules used to pair Participants and determine Advancement.</summary>
    public required MatchFormat Format { get; init; }
    /// <summary>Head-to-head requires 1 or 3; omit for free-for-all.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BestOf { get; init; }
    /// <summary>Free-for-all requires 1–100; omit for head-to-head.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Bouts { get; init; }
    /// <summary>1–1024 Groups, default 1. Free-for-all Groups must fit the Game's Maximum Match Size.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Groups { get; init; }
    /// <summary>Positive Participants per Group advancing when another Stage follows; omit or set 0 on the last Stage.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Advancement { get; init; }
    /// <summary>Swiss only: 0–100; omit or set 0 for the default Round count.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SwissRounds { get; init; }
    /// <summary>Time in seconds a Match has to complete from Ready, 60–604800.</summary>
    public required int ResultDeadlineSeconds { get; init; }
}
