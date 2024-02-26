namespace OpenTournament.Common.Draw.Layout;

public sealed record LocalMatch(int Round, int MatchId, int Opponent1 = -1, int Opponet2 = -1);

// <summary>
// Create list of matches with matchId
// </summary>
public sealed class LocalMatches
{
    public DrawSize DrawSize { get; init; }
   
    public List<LocalMatch> Matches { get; set; } = new();

    private int _matchId = 1;
    
    public LocalMatches(FirstRoundPositions positions)
    {
        DrawSize = positions.DrawSize;
        CreateFirstRoundMatches(positions);

        int totalRounds = DrawSize.ToTotalRounds();
        if (totalRounds > 1) {
            CreateNextRoundMatches(totalRounds);
        }
    }

    private void CreateFirstRoundMatches(FirstRoundPositions positions)
    {
        foreach (var match in positions.Matches)
        {
            AddMatch(new(1, _matchId, 
                match.FirstParticipant,
                match.SecondParticipant) );
        }
    }
   
    private void CreateNextRoundMatches(int totalRounds)
    {
        for (int round = 2; round <= totalRounds; round++)
        {
            for (int i = 0; i < GetTotalMatchesInRound(round); i++)
            {
                AddMatch(new(round, _matchId));
            }
        }
    }

    private void AddMatch(LocalMatch match)
    {
        Matches.Add(match);
        _matchId++;
    }
   
    int GetTotalMatchesInRound(int round) => (int)DrawSize.Value / (int)Math.Pow(2, round);
}
