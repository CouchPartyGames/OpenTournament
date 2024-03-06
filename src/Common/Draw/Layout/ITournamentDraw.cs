namespace OpenTournament.Common.Draw.Layout;

public interface ITournamentDraw
{
   bool HasWinProgressionMatch(int currentMatchId);
   
   int GetWinProgressionMatchId(int currentMatchId);

   bool HasLoseProgressionMatch(int currentMatchId);
   
   int GetLoseProgressionMatchId(int currentMatchId);
   
   bool IsTournamentComplete();
}
