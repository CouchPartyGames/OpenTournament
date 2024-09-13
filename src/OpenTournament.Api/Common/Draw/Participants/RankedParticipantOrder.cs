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
    public override Dictionary<int, Participant> Opponents
    {
        get
        {
            if (opponents.Count < MinParticipants)
            {
                throw new LackOfOpponentsException("Not enough participants");
            }

            int i = _startIndex;
            var orderedOpponents = new Dictionary<int, Participant>();
            foreach (var opp in opponents.OrderByDescending(o => o.Rank))
            {
                orderedOpponents.Add(i, opp);
                i++;
            }

            return orderedOpponents;
        }
    }

}