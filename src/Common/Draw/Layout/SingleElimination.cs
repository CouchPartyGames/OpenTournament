using OpenTournament.Common.Draw.Participants;
using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Layout;

/*
public sealed class SingleElimination
{
    public SingleElimination(SingleEliminationLayout layout, ParticipantOrder participantOrder)
    {
            // Group by Round
            // Order by Round Then Match Id
        var matchesByRound = layout
            .Matches
            .GroupBy(m => m.Round)
            .OrderBy(m => m.Key)
            .Select(m =>
            {
                return new { Round = m.Key, Matches = m.ToList() };
            });
        
            // Create Matches
            // If First Round, Add Participants
        int position = 1;
        foreach (var item in matchesByRound)
        {
            if (item.Round == 1)
            {
                SetRoundOne(item.Matches);
            }
            else
            {
                SetRound(item.Matches, item.Round);                
            }
        }
    }


    void SetRoundOne(List<LocalMatch> matches)
    {
        foreach (var match in matches)
        {
            int position1 = match.Opponent1;
            int position2 = match.Opponent2;
            var participant1 = null;
            var participant2 = null;
            
            if (!participantOrder.Opponents.TryGetValue(position1, out participant1))
                participant1 = SetBye()

            if (!participantOrder.Opponents.TryGetValue(position2, out participant2))
                participant2 = SetBye();
            
            AddMatch()
        }
    }

    void SetRound(List<LocalMatch> matches, int round)
    {
        AddMatch(round, matchId, winProgression);
    }
}

public sealed class SingleEliminationExisting
{
    public SingleEliminationExisting(SingleEliminationLayout layout, List<Match> matches)
    {
        
        var matchesByRound = layout
            .Matches
            .GroupBy(m => m.Round)
            .OrderBy(m => m.Key)
            .Select(m =>
            {
                return new { Round = m.Key, Matches = m.ToList() };
            });

        foreach (var item in matchesByRound)
        {
            // Loop thru matches
            foreach (var match in item.Matches)
            {
                var foundMatch = FindMatchByMatchId(match.MatchId)
                if (foundMatch is null)
                {
                    // Match doesn't exist
                    // Use layout
                    AddMatch();
                }
                else
                {
                    // Exists in DB
                    
                }
            }
        }
    }

    void FindMatchByMatchId()
    {
        
        foreach()
    }
    
    
}*/