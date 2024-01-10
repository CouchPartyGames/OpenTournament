namespace OpenTournament.Common.Draw.Layout;

public class LocalMatchIds(DrawSize drawSize)
{
   private readonly DrawSize drawSize = drawSize;
    
   public Dictionary<int, List<int>> CreateMatchIds()
   {
      int localMatchId = 1;
      var ids = new List<int>();
      
      // round number = list of match ids
      Dictionary<int, List<int>> matchIds = new();
      for (int round = 1; round <= drawSize.ToTotalRounds(); round++)
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
   
   int GetTotalMatchesInRound(int round) => (int)drawSize.Value / (int)Math.Pow(2, round);
}