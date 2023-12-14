using OpenTournament.Common.Draw.Opponents;

namespace OpenTournament.Common.Draw.Layout;

public sealed record SeedMatch(int Id, Opponent Opponent1, Opponent Opponent2);

public sealed class CreateMatches
{
    private Dictionary<OpponentOrder, Opponent> _opponents;
    private List<VersusMatch> _draw;
    private Dictionary<int, SeedMatch> Matches { get; } = new();
    
    public CreateMatches(Dictionary<OpponentOrder, Opponent> opponents,
        List<VersusMatch> draw)
    {
        _opponents = opponents;
        _draw = draw;
    }

    
    void AddMatch()
    {
        int id = 1;

        foreach(var versusMatch in _draw)
        {
            if (!_opponents.TryGetValue(new OpponentOrder(versusMatch.FirstParticipant - 1), 
                    out Opponent opponent1))
            {
                opponent1 = Opponent.CreateBye();
            }
            
            if (!_opponents.TryGetValue(new OpponentOrder(versusMatch.SecondParticipant - 1), 
                    out Opponent opponent2))
            {
                opponent2 = Opponent.CreateBye();
            }

            Matches.Add(id, new SeedMatch(id, opponent1, opponent2) );
            id++;
        }
    }
}