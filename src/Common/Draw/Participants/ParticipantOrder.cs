namespace OpenTournament.Common.Draw.Participants;

public sealed class EmptyListOfOpponentsException(string message) : Exception(message);

public abstract class ParticipantOrder
{
    protected const int _startIndex = 0;
    
    public enum Order
    {
        Random = 0,
        Ranked
    };

    public abstract Dictionary<OpponentOrder, Opponent> Opponents { get; }
    
    public static ParticipantOrder Create(Order order, List<Opponent> opponents)
    {
        return order switch
        {
            Order.Random => new RandomParticipantOrder(opponents),
            Order.Ranked => new RankedParticipantOrder(opponents)
        };
    }
}