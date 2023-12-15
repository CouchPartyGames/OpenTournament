namespace OpenTournament.Common.Draw.Participants;

public sealed class EmptyListOfOpponentsException(string message) : Exception(message);

public interface IParticipantOrder
{
    public Dictionary<OpponentOrder, Opponent> Opponents { get; }
}

public abstract class ParticipantOrder
{
    public enum Order
    {
        Random = 0,
        Ranked
    };

    public static IParticipantOrder Create(Order order, List<Opponent> opponents)
    {
        return order switch
        {
            Order.Random => new RandomParticipantOrder(opponents),
            Order.Ranked => new RankedParticipantOrder(opponents)
        };
    }
}