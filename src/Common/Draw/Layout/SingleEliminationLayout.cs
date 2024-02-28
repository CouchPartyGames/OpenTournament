using Grpc.Net.Client.Configuration;

namespace OpenTournament.Common.Draw.Layout;

public sealed record LocalMatch(int Round, int MatchId, int Opponent1 = -1, int Opponent2 = -1);
public sealed record SingleElimationMatch(int Round, int MatchId, int Opponent1, int Opponent2, int WinProgressionMatchId);

// <summary>
// Create list of matches with matchId
// </summary>
public sealed partial class SingleEliminationLayout
{
    public DrawSize DrawSize { get; init; }
   
    public List<LocalMatch> Matches { get; set; } = new();
    
    public FinalsType FinalsType { get; init; }

    private int _matchId = 1;
    
    public SingleEliminationLayout(FirstRoundPositions positions, FinalsType finalsType = FinalsType.OneOfOne)
    {
        DrawSize = positions.DrawSize;
        FinalsType = finalsType;
        
        int totalRounds = DrawSize.ToTotalRounds();
        CreateFirstRoundMatches(positions);
        if (totalRounds > 1) {
            CreateNextRoundMatches(totalRounds);
        }

        var ids = new CreateMatchIds(positions);
        //ids.MatchByIds
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
        CreateFinalRoundMatches(totalRounds);
    }

    private void CreateFinalRoundMatches(int round)
    {
        switch (FinalsType)
        {
            case FinalsType.TwoOfThree:
                AddMatch(new LocalMatch(round + 1, _matchId));
                AddMatch(new LocalMatch(round + 2, _matchId));
                break;
            case FinalsType.ThreeOfFive:
                AddMatch(new LocalMatch(round + 1, _matchId));
                AddMatch(new LocalMatch(round + 2, _matchId));
                AddMatch(new LocalMatch(round + 3, _matchId));
                AddMatch(new LocalMatch(round + 4, _matchId));
                break;
        }
    }

    private void AddMatch(LocalMatch match)
    {
        Matches.Add(match);
        _matchId++;
    }
   
    // Does not include 2of3, 3of5
    int GetTotalMatchesInRound(int round) => (int)DrawSize.Value / (int)Math.Pow(2, round);

    // Does not include 2of3, 3of5 Finals
    int GetTotalMatches(int matches)
    {
        int nextMatches = matches / 2;
        if (nextMatches == 1) {
            return 1;
        }

        return GetTotalMatches(nextMatches) + nextMatches;
    }
}
