using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Layout;


public sealed class SingleEliminationFirstRound
{
   public sealed record SingleMatch(int Round, int MatchId, Participant Opp1, Participant Opp2, int WinMatchId)
   {
      public static SingleMatch Create(CreateProgressionMatches.ProgressionMatch match, Participant Opp1, Participant Opp2)
      {
         return new(match.Round, match.MatchId, Opp1, Opp2, match.WinMatchId);
      }
   }

   public Dictionary<int, Participant> Participants { get; init; }

   public List<SingleMatch> Matches { get; init; }
   
   public SingleEliminationFirstRound(List<CreateProgressionMatches.ProgressionMatch> matches, ParticipantOrder participants)
   {
      Participants = participants.Opponents;
      
         // Merge Participants and Matches in First Round
      matches
         .Where(x => x.Round == 1)
         .ToList()
         .ForEach(e =>
      {
         Matches.Add(SingleMatch.Create(e, AssignOpponent(e.Position1), AssignOpponent(e.Position2) ));
      });
   }

   Participant AssignOpponent(int position) => 
      Participants.ContainsKey(position) ? Participants[position] : Participant.CreateBye();
}