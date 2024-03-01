using System.Reflection.Metadata;
using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Layout;


public sealed class SingleEliminationDraw2
{
   public sealed record Match(int Round, int MatchId, Participant Opp1, Participant Opp2, int WinMatchId)
   {
      public static Match Create(SingleElimationMatch match, Participant Opp1, Participant Opp2)
      {
         return new(match.Round, match.MatchId, Opp1, Opp2, match.WinProgressionMatchId);
      }
   }

   public Dictionary<int, Participant> Participants { get; init; }

   public List<Match> Matches { get; init; }
   
   public SingleEliminationDraw2(List<SingleElimationMatch> matches, ParticipantOrder participants)
   {
      Participants = participants.Opponents;
      
         // Merge Participants and Matches in First Round
      matches
         .Where(x => x.Round == 1)
         .ToList()
         .ForEach(e =>
      {
         Matches.Add(Match.Create(e, AssignOpponent(e.Opponent1), AssignOpponent(e.Opponent2) ));
      });
      
      var otherRounds = matches
         .Where(x => x.Round != 1)
         .OrderBy(x => x.MatchId)
         .ToList();

   }

   Participant AssignOpponent(int position) => 
      Participants.ContainsKey(position) ? Participants[position] : Participant.CreateBye();
   
   /*
   List<SingleElimationMatch> GetMatchesInRound(int round)
   {
      return matches
         .Where(x => x.Round == round)
         .OrderBy(x => x.MatchId)
         .ToList();
   }*/
}