namespace OpenTournament.Common;

public sealed record MatchId(Guid Value);

public enum MatchStatus
{
    Ready = 0,
    InProgress,
    Complete
};

public sealed class Match
{
    public MatchId Id { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Ready;
}