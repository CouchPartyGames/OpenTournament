namespace OpenTournament.Common.Draw.Participants;

// <summary>
// Ranked Order of Participants
// </summary>
public sealed class RankedParticipantOrder(List<Opponent> opponents) : IParticipantOrder
{

    private const int _startIndex = 0;
    
    // <summary>
    // Dictionary of ordered opponents
    // </summary>
    public Dictionary<OpponentOrder, Opponent> Opponents
    {
        get
        {
            if (opponents.Count < 2)
            {
                throw new EmptyListOfOpponentsException("Not enough participants");
            }

            int i = _startIndex;
            var orderedOpponents = new Dictionary<OpponentOrder, Opponent>();
            foreach (Opponent opp in opponents.OrderByDescending(o => o.Rank))
            {
                orderedOpponents.Add(new OpponentOrder(i), opp);
                i++;
            }

            return orderedOpponents;
        }
    }

}