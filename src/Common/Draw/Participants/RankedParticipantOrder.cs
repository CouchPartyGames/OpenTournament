using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Participants;

// <summary>
// Ranked Order of Participants
// </summary>
public sealed class RankedParticipantOrder(List<Participant> opponents) : ParticipantOrder
{
    // <summary>
    // Dictionary of ordered opponents
    // </summary>
    public override Dictionary<OpponentOrder, Participant> Opponents
    {
        get
        {
            if (opponents.Count < 2)
            {
                throw new EmptyListOfOpponentsException("Not enough participants");
            }

            int i = _startIndex;
            var orderedOpponents = new Dictionary<OpponentOrder, Participant>();
            foreach (var opp in opponents.OrderByDescending(o => o.Rank))
            {
                orderedOpponents.Add(new OpponentOrder(i), opp);
                i++;
            }

            return orderedOpponents;
        }
    }

}