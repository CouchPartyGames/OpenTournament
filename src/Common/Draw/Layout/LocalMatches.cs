namespace OpenTournament.Common.Draw.Layout;

public sealed record LocalMatch(int Round, int MatchId, int Opponent1 = -1, int Opponet2 = -1);

// <summary>
// Create list of matches with matchId
// </summary>
public sealed class LocalMatches
{
    public DrawSize DrawSize { get; init; }
   
    public List<LocalMatch> Matches { get; set; } = new();

    public LocalMatches(FirstRoundPositions positions)
    {
        DrawSize = positions.DrawSize;
        CreateMatchIds(positions);
    }

   
    public void CreateMatchIds(FirstRoundPositions positions)
    {
        int matchId = 1;
        int totalRounds = DrawSize.ToTotalRounds(); 

        foreach (var match in positions.Matches)
        {
            Matches.Add(new(1, matchId, match.FirstParticipant,match.SecondParticipant) );
            matchId++;
        }

        if (totalRounds > 1)
        {
            for (int round = 2; round <= totalRounds; round++)
            {
                Matches.Add(new(round, matchId)); 
                matchId++;
            }
        }
    }
   
    int GetTotalMatchesInRound(int round) => (int)DrawSize.Value / (int)Math.Pow(2, round);
}