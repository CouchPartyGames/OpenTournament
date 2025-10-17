using CouchPartyGames.TournamentGenerator.Type;

namespace OpenTournament.Api.Data.Models;

public class MatchProgressions
{
    public Dictionary<int, LocalMatchId> WinProgressions;
    
    public Dictionary<int, LocalMatchId> LoseProgressions;
}