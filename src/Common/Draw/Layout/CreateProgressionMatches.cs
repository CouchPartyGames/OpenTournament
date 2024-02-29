namespace OpenTournament.Common.Draw.Layout;

public sealed class CreateProgressionMatches
{
    public List<SingleEliminationMatch> MatchWithProgressions { get; private set; } = new();
    
    public sealed record SingleEliminationMatch(int Round, int MatchId, int WinMatchId, int Position1 = -1, int Position2 = -1)
    {
        public static SingleEliminationMatch Create(CreateMatchIds.MatchWithId match, int winMatchId) => 
            new(match.Round, match.MatchId, winMatchId, match.Position1, match.Position2);
    }
    
    public CreateProgressionMatches(List<CreateMatchIds.MatchWithId> matches)
    {
        int round = 1;
        int nextRound = 2;

        int totalRounds = matches.Max(m => m.Round);
        if (totalRounds == 1)
        {
            SingleRound(matches[0]);
            return;
        }

        for (round = 1; round < totalRounds; round++)
        {
            nextRound = round + 1;
            
            // Chunk the Current Round (pair of 2 matches = 1 chunk)
            var currentRoundMatches = matches
                .Where(m => m.Round == round)
                .Chunk(2)
                .ToList();

            // Get Next Round
            var nextRoundMatches = matches
                .Where(m => m.Round == nextRound)
                .ToList();
            
            // Verify Chuck has the same as Next Round Matches
            if (currentRoundMatches.Count != nextRoundMatches.Count)
            {
                throw new Exception("Progression number of chunked didn't match next round's match count");
            }

            
            int i = 0;
            // Assign Progression
            foreach (var next in nextRoundMatches)
            {
                var winMatchId = next.MatchId;
                // element #1 in chunk
                AddProgression(currentRoundMatches[i][0], winMatchId);  
                // element #2 in chunk
                AddProgression(currentRoundMatches[i][1], winMatchId);
                i++;
            }
        }
        SingleRound(matches.Last());
    }

    void SingleRound(CreateMatchIds.MatchWithId match)
    {
        MatchWithProgressions.Add(SingleEliminationMatch.Create(match, -1));             
    }
    
    void AddProgression(CreateMatchIds.MatchWithId match, int winMatchId)
    {
        MatchWithProgressions.Add(SingleEliminationMatch.Create(match, winMatchId));
    }
}