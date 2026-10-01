namespace AgonesTournament.Sdk.Core;

/// <summary>One Tournament with settings, configured Stages and registration counts.</summary>
public sealed record Tournament
{
    /// <summary>The maximum number of Participants accepted by this Tournament.</summary>
    public required int Capacity { get; init; }
    /// <summary>Whether Check-in is required and its Window length.</summary>
    public required CheckInSettings CheckIn { get; init; }
    /// <summary>When the Check-in Window opens; absent when Check-in is disabled.</summary>
    public DateTimeOffset? CheckInOpensAt { get; init; }
    /// <summary>The number of Participants who have checked in.</summary>
    public required int CheckedIn { get; init; }
    /// <summary>When play completed; absent until completion.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>When the Tournament was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
    /// <summary>The Game catalog identifier.</summary>
    public required string GameId { get; init; }
    /// <summary>The UUID of this domain object.</summary>
    public required TournamentId Id { get; init; }
    /// <summary>The number required at the start to avoid cancellation.</summary>
    public required int MinimumParticipants { get; init; }
    /// <summary>The Tournament name chosen by the Organizer.</summary>
    public required string Name { get; init; }
    /// <summary>The Organizer: user:&lt;keycloak subject&gt; or client:&lt;keycloak client id&gt;.</summary>
    public required string Organizer { get; init; }
    /// <summary>Participants registered, checked in or playing.</summary>
    public required int Registered { get; init; }
    /// <summary>When the Registration Window opens.</summary>
    public required DateTimeOffset RegistrationOpensAt { get; init; }
    /// <summary>The configured Stages in play order; the API may return null.</summary>
    public required IReadOnlyList<ConfiguredStage>? Stages { get; init; }
    /// <summary>When the Tournament starts and registration closes.</summary>
    public required DateTimeOffset StartsAt { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>When the Tournament was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>A page of Tournaments; the API may return a null collection.</summary>
public sealed record TournamentPage
{
    /// <summary>The Tournaments on this page; the API may return null.</summary>
    public required IReadOnlyList<Tournament>? Tournaments { get; init; }
}

/// <summary>Whether Check-in is required and the length of its Window.</summary>
public sealed record CheckInSettings
{
    /// <summary>Participants must check in or are dropped at the start.</summary>
    public required bool Enabled { get; init; }
    /// <summary>Length of the Check-in Window, ending at the start.</summary>
    public int? WindowSeconds { get; init; }
}

/// <summary>A configured Stage, including its Format and progression settings.</summary>
public sealed record ConfiguredStage
{
    /// <summary>Participants per Group who advance to the next Stage; 0 on the last Stage.</summary>
    public int? Advancement { get; init; }
    /// <summary>Head-to-head Formats: Bouts per Match.</summary>
    public int? BestOf { get; init; }
    /// <summary>Free-for-all: Bouts per Match.</summary>
    public int? Bouts { get; init; }
    /// <summary>The rules used to pair Participants and determine advancement.</summary>
    public required MatchFormat Format { get; init; }
    /// <summary>Groups the Stage is split into.</summary>
    public int? Groups { get; init; }
    /// <summary>The UUID of this domain object.</summary>
    public required StageId Id { get; init; }
    /// <summary>The one-based position within the containing Tournament or Stage.</summary>
    public required int Position { get; init; }
    /// <summary>Time a Match has to complete, from when it is Ready.</summary>
    public required int ResultDeadlineSeconds { get; init; }
    /// <summary>The current lifecycle state.</summary>
    public required StageStatus Status { get; init; }
    /// <summary>Swiss: overrides the default ⌈log₂ N⌉ Rounds.</summary>
    public int? SwissRounds { get; init; }
}
