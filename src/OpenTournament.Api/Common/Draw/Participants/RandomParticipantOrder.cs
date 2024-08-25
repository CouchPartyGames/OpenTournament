using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Participants;

// <summary>
// Random ordering of Participants (Blind Draw Seeding)
// </summary>
public sealed class RandomParticipantOrder(List<Participant> opponents) : ParticipantOrder
{
    // <summary>
    // Dictionary of ordered opponents
    // </summary>
    public override Dictionary<int, Participant> Opponents
    {
        get
        {
            if (opponents.Count < 2)
            {
                throw new EmptyListOfOpponentsException("Not enough participants");
            }
            
            Random rng = new Random();
            var orderedOpps = new Dictionary<int, Participant>();

            int i = _startIndex;
            foreach (var opp in opponents.OrderBy(a => rng.Next()).ToList() )
            {
                orderedOpps.Add(i, opp);
                i++;
            }

            return orderedOpps;
        }
    }
}