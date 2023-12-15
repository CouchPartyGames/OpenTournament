namespace OpenTournament.Common.Draw.Layout;

public enum RoundId
{
   Finals = 1,
   Semifinals = 2,
   Quarterfinals = 3,
   RoundOf16 = 4
};

public record FullMatch(int Id, int ParticipantPos);

public sealed class SingleEliminationDraw
{
   public int NumRounds { get; private set;  }
   
   private List<VersusMatch> _firstMatches;
   public FullDraw(List<VersusMatch> firstMatches)
   {
      _firstMatches = firstMatches;
      NumRounds = firstMatches.Count;
   }

   public bool Create()
   {
      int i = 1;
      int numMatches = _firstMatches.Count;
      foreach (var match in _firstMatches)
      {
         new FullMatch(i);
         i++;
      }
   }

   public static int MatchCountToNumRounds(int numMatches) => numMatches switch
   {
      1 => 1,
      2 => 2,
      4 => 3,
      8 => 4,
      16 => 5,
      32 => 6,
      64 => 7,
      128 => 8
   };
   
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