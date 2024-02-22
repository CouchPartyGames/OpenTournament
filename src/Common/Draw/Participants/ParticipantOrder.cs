using OpenTournament.Data.Models;

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

    public abstract Dictionary<OpponentOrder, Participant> Opponents { get; }
    
    public static ParticipantOrder Create(Order order, List<Participant> opponents)
    {
        return order switch
        {
            Order.Ranked => new RankedParticipantOrder(opponents),
            _ => new RandomParticipantOrder(opponents)
        };
    }
}