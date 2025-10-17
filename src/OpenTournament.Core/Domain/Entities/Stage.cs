using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Api.Data.Models;

[Table("Stages")]
public sealed class Stage
{
    public enum State
    {
        Ready,
        InProgress,
        Completed
    }
    
    public required StageId StageId { get; set;  }
    
    public string Name = String.Empty;
    
    public List<Pool> Pools = [];
    
    public int Order;

    public State StageState = State.Ready;

    public DateTime CompletedOnUtc;
    
    //public List<IProgression> Progressions = [];
}
