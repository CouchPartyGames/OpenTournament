namespace OpenTournament.Common.Draw.Layout;


public enum FinalsType
{
   OneOfOne = 0,
   TwoOfThree,
   ThreeOfFive
};


public sealed class CurrentDoesntMatchNextException(string message) : Exception(message);

public sealed class SingleEliminationDraw
{
   private readonly DrawSize _drawSize;
   private readonly int _totalRounds;
   private readonly FinalsType _finalsType;
   
   private List<VersusMatch> _positions;

   public Dictionary<int, DrawMatch> Matches
   {
      get { return _matches; }
   }

   private Dictionary<int, DrawMatch> _matches = new();

   public List<DrawMatch> GetMatchesInRound(int round)
   {
      return _matches
         .Where(x => x.Value.Round == round)
         .Select(x => x.Value)
         .ToList();
   }
   
   public SingleEliminationDraw(ParticipantPositions positions, FinalsType finalsType = FinalsType.OneOfOne)
   {
      _positions = positions.Matches;
      _drawSize = positions.DrawSize;
      _finalsType = finalsType;
      _totalRounds = _drawSize.ToTotalRounds();
   }

   public void CreateMatchProgressions(Dictionary<int, List<int>> matchIds)
   {
      for (int round = 1; round < _totalRounds; round++)
      {
         if (!matchIds.ContainsKey(round) || !matchIds.ContainsKey(round + 1))
         {
            continue;
         }
         
            // Get Match Ids for the Current and Next Round (after current round)
         AddMatches(round, matchIds[round], matchIds[round + 1]);
      }
      
         // ToDo: Verify Last Match
      AddFinalsMatches(matchIds[_totalRounds][0], _totalRounds);
   }

   void AddMatches(int round, List<int> curMatchIds, List<int> nextMatchIds)
   {
      int prevId = 0;
      var chunkPairs = curMatchIds.Chunk<int>(2);

         // Number of Pairs in the Current should match the Next Round
      if (chunkPairs.ToList().Count != nextMatchIds.Count)
      {
         throw new CurrentDoesntMatchNextException("Mismatch Current with Next Round");
      }
      
      foreach (var pair in chunkPairs)
      {
         var progressMatchId = nextMatchIds.ToArray()[prevId];
         foreach (var matchId in pair)
         {
            if (round == 1)
            {
               var match = new DrawMatch(matchId, 
                  progressMatchId, 
                  _positions[matchId - 1].FirstParticipant,
                  _positions[matchId - 1].SecondParticipant);
               
               _matches.Add(matchId, match);
            }
            else
            {
               var match = new DrawMatch(matchId, round, progressMatchId);
               _matches.Add(matchId, match);
            }

         }
         prevId++;
      }
   }

   void AddFinalsMatches(int matchId, int round)
   {
      switch (_finalsType)
      {
         case FinalsType.OneOfOne:
            _matches.Add(matchId, new DrawMatch(matchId, round));
            break;
         case FinalsType.TwoOfThree:
            _matches.Add(matchId, new DrawMatch(matchId, round, matchId + 1));
            _matches.Add(matchId + 1, new DrawMatch(matchId + 1, round + 1, matchId + 2));
            _matches.Add(matchId + 2, new DrawMatch(matchId + 2, round + 2));
            break;
         case FinalsType.ThreeOfFive:
            _matches.Add(matchId, new DrawMatch(matchId, round, matchId + 1));
            _matches.Add(matchId + 1, new DrawMatch(matchId + 1, round + 1, matchId + 2));
            _matches.Add(matchId + 2, new DrawMatch(matchId + 2, round + 2, matchId + 3));
            _matches.Add(matchId + 3, new DrawMatch(matchId + 3, round + 3, matchId + 4));
            _matches.Add(matchId + 4, new DrawMatch(matchId + 4, round + 4));
            break;
      }
   }
}