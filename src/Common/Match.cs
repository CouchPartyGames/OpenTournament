namespace OpenTournament.Common;

public sealed record MatchId(Guid Value);

public sealed class Match
{
    public MatchId Id { get; set; }
}