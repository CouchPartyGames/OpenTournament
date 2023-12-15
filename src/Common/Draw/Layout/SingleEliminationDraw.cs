using System.Net.Sockets;

namespace OpenTournament.Common.Draw.Layout;

public enum RoundId
{
   Finals = 1,
   Semifinals = 2,
   Quarterfinals = 3,
   RoundOf16 = 4
};

public sealed record FullMatch(int Id, int RoundId, int Progression);

public sealed class SingleEliminationDraw
{
   private readonly DrawSize _drawSize;
   private readonly int _totalRounds;
   
   private List<VersusMatch> _positions;

   private Dictionary<int, FullMatch> _matches = new();
   public int _matchId = 1;
   
   public SingleEliminationDraw(ParticipantDrawPositions postions)
   {
      _positions = postions.Matches;
      _drawSize = postions.DrawSize;
      _totalRounds = postions.DrawSize.ToTotalRounds();
   }

   
   public Dictionary<int, List<int>> CreateMatchIds()
   {
      int localId = 1;
      Dictionary<int, List<int>> matchIds = new();
      for (int i = 1; i <= _totalRounds; i++)
      {
         var totalMatches = GetTotalMatchInRound(i);
         var ids = new List<int>();
         for (int j = 0; j < totalMatches; j++)
         {
            ids.Add(localId);
            localId++;
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
            throw new Exception("Invalid number of matches");
         }
         
            // Loop thru the Sets in the Current Round
         foreach (var pair in chunkPairs)
         {
            var progressMatchId = nextMatchIds[prevId];
            foreach (var matchId in pair)
            {
               new FullMatch(matchId, round, progressMatchId);
            }
            prevId++;
         }
      }
   }


   public int GetTotalMatchInRound(int round) => _drawSize.Value / (int)Math.Pow(2, round);
   
   
   public static RoundId MatchCountToRoundId(int numMatches) => numMatches switch
   {
      1 => RoundId.Finals,
      2 => RoundId.Semifinals,
      4 => RoundId.Quarterfinals,
      8 => RoundId.RoundOf16
   };

   public static string RoundToName(RoundId round) => round switch
   {
      RoundId.Finals => "Finals",
      RoundId.Semifinals => "Semifinals",
      RoundId.Quarterfinals => "Quarterfinals",
      RoundId.RoundOf16 => "Round of 16"
   };
   
}