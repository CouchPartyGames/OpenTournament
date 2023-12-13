namespace OpenTournament.Common.Draw.Opponents;

public sealed record Opponent(string Id, int Rank);

public sealed record OpponentOrder(int Value);

public enum DrawOrder
{
    BlindDraw,
    SeededDraw
};

public interface IOpponentDrawOrder
{
    public Dictionary<OpponentOrder, Opponent> Opponents { get; }
}

/*
public sealed class OpponentDrawOrdering(List<Opponent> opponents, DrawOrder order)
{
    public Dictionary<OpponentOrder, Opponent> Opponents
    {
        get
        {
            var ordering = order switch
            {
                DrawOrder.BlindDraw => new BlindDrawOrdering(opponents),
                DrawOrder.SeededDraw => new SeededDrawOrdering(opponents)
            };
            return ordering.Opponents;
        }
    }
}
*/