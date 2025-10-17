namespace OpenTournament.Api.Data.Models;

public sealed record TournamentId(Guid Value)
{
    public static TournamentId? TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new(guid);
    }
    
    public static TournamentId NewTournamentId() => new(Guid.CreateVersion7());
}