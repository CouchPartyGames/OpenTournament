namespace OpenTournament.Common.Draw.Layout;

public enum RoundId
{
   Finals = 1,
   Semifinals = 2,
   Quarterfinals = 3,
   RoundOf16 = 4
};

public enum FinalsType
{
   OneOfOne,
   TwoOfThree,
   ThreeOfFive
};

public enum EliminationMode
{
   Single = 0,
   Double
};

public sealed record DrawMatch(int Id, int RoundId, int Progression, int Position1 = 0, int Position2 = 0);

public sealed class CurrentDoesntMatchNextException(string message) : Exception(message);

public sealed class SingleEliminationDraw
{
   private readonly DrawSize _drawSize;
   private readonly int _totalRounds;
   
   private List<VersusMatch> _positions;

   public Dictionary<int, DrawMatch> Matches
   {
      get { return _matches; }
   }

   private Dictionary<int, DrawMatch> _matches = new();
   
   public SingleEliminationDraw(ParticipantPositions postions, FinalsType finalsType = FinalsType.OneOfOne)
   {
      _positions = postions.Matches;
      _drawSize = postions.DrawSize;
      _totalRounds = postions.DrawSize.ToTotalRounds();

      CreateMatchProgressions(CreateMatchIds());
   }

   
   public Dictionary<int, List<int>> CreateMatchIds()
   {
      int localMatchId = 1;
      
      // round number = list of match ids
      Dictionary<int, List<int>> matchIds = new();
      for (int i = 1; i <= _totalRounds; i++)
      {
         var totalMatches = GetTotalMatchesInRound(i);
         var ids = new List<int>();
         for (int j = 0; j < totalMatches; j++)
         {
            ids.Add(localMatchId);
            localMatchId++;
         }
         matchIds.TryAdd(i, ids);
      }
      
      return matchIds;
   }

   public void CreateMatchProgressions(Dictionary<int, List<int>> matchIds)
   {
      int prevId = 0;
      int round = 0;
      for (round = 1; round < _totalRounds; round++)
      {
            // Get Match Ids for the Current and Next Round (after current round)
         var curMatchIds = matchIds[round];
         var nextMatchIds = matchIds[round + 1];


            // Create Sets of 2 (MatchIds) 
         var chunkPairs = curMatchIds.Chunk<int>(2);

            // Number of Pairs in the Current should match the Next Round
         if (chunkPairs.ToList().Count != nextMatchIds.Count)
         {
            throw new CurrentDoesntMatchNextException("Mismatch Current with Next Round");
         }
         
            // Loop thru the Sets in the Current Round
         foreach (var pair in chunkPairs)
         {
            var progressMatchId = nextMatchIds[prevId];
            foreach (var matchId in pair)
            {
               _matches.Add(matchId, new DrawMatch(matchId, round, progressMatchId));
            }
            prevId++;
         }
      }
   }

   public int GetTotalMatchesInRound(int round) => (int)_drawSize.Value / (int)Math.Pow(2, round);
   
   
   public static RoundId MatchCountToRoundId(int numMatches) => numMatches switch
   {
      1 => RoundId.Finals,
      2 => RoundId.Semifinals,
      4 => RoundId.Quarterfinals,
      8 => RoundId.RoundOf16,
      _ => throw new Exception("bad rounds")
   };
}