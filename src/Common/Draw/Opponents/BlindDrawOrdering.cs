namespace OpenTournament.Common.Draw.Opponents;

public sealed class BlindDrawOrdering(List<Opponent> opponents) : IOpponentDrawOrder
{
    public Dictionary<OpponentOrder, Opponent> Opponents
    {
        get
        {
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