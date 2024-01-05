namespace OpenTournament.Common.Draw.Layout;

public class LocalMatchIds(DrawSize drawSize)
{
   private readonly DrawSize _drawSize = drawSize;
    
   public Dictionary<int, List<int>> CreateMatchIds()
   {
      int localMatchId = 1;
      var ids = new List<int>();
      
      // round number = list of match ids
      Dictionary<int, List<int>> matchIds = new();
      for (int round = 1; round <= _drawSize.ToTotalRounds(); round++)
      {
         var totalMatches = GetTotalMatchesInRound(round);
         for (int j = 0; j < totalMatches; j++)
         {
            ids.Add(localMatchId);
            localMatchId++;
         }
         matchIds.TryAdd(round, [..ids]);
         ids.Clear();
      }
      
      return matchIds;
   }
   
   int GetTotalMatchesInRound(int round) => (int)_drawSize.Value / (int)Math.Pow(2, round);
}