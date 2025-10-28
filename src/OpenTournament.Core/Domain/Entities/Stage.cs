using System.ComponentModel.DataAnnotations.Schema;
using OpenTournament.Api.Data.Models;
using OpenTournament.Core.Domain.ValueObjects;

namespace OpenTournament.Core.Domain.Entities;

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
