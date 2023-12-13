namespace OpenTournament.Common.Draw.Opponents;

public sealed class SeededDrawOrdering(List<Opponent> opponents) : IOpponentDrawOrder
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