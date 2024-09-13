using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Participants;

public sealed class LackOfOpponentsException(string message) : Exception(message);

public abstract class ParticipantOrder
{
    protected const int _startIndex = 1;

    protected const int MinParticipants = 2;
    
    public enum Order
    {
        Random = 0,
        Ranked
    };

    public abstract Dictionary<int, Participant> Opponents { get; }

    public static ParticipantOrder Create(Order order, List<Participant> participants) => order switch
    {
        Order.Ranked => new RankedParticipantOrder(participants),
        _ => new RandomParticipantOrder(participants)
    };
}