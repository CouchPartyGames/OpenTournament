using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

[Table("Pools")]
public sealed class Pool
{
    public enum State
    {
        Ready,
        InProgress,
        Completed
    }
    
    public required PoolId PoolId;
    
    public string Name = String.Empty;

    public CompetitionType CompType;
    
    public int Order;

    public State PoolState = State.Ready;
    
    //public List<IProgression> Progressions = [];
}
