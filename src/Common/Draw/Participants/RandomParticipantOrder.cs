namespace OpenTournament.Common.Draw.Participants;

// <summary>
// Random ordering of Participants (Blind Draw Seeding)
// </summary>
public sealed class RandomParticipantOrder(List<Opponent> opponents) : IParticipantOrder
{
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
            
            Random rng = new Random();
            var orderedOpps = new Dictionary<OpponentOrder, Opponent>();

            int i = 0;
            foreach (Opponent opp in opponents.OrderBy(a => rng.Next()).ToList() )
            {
                orderedOpps.Add(new OpponentOrder(i), opp);
                i++;
            }

            return orderedOpps;
        }
    }
}