using CouchPartyGames.TournamentGenerator.Type;

namespace OpenTournament.Core.Domain.Entities;

public class MatchProgressions
{
    public Dictionary<int, LocalMatchId> WinProgressions;
    
    public Dictionary<int, LocalMatchId> LoseProgressions;
}