namespace OpenTournament.Common.Draw.Participants;

// <summary>
// Ranked Order of Participants
// </summary>
public sealed class RankedParticipantOrder(List<Opponent> opponents) : IParticipantOrder
{

    // <summary>
    // Dictionary of ordered opponents
    // </summary>
    public Dictionary<OpponentOrder, Opponent> Opponents
    {
        get
        {
            int i = 0;
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