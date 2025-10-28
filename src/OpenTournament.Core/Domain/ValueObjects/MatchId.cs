namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record MatchId(Guid Value)
{
    public static MatchId? TryParse(string id) => !Guid.TryParse(id, out Guid guid) ? null : new MatchId(guid);

    public static MatchId NewMatchId() => new (Guid.CreateVersion7());
}