using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Data.Models;

public sealed record ParticipantId(string Value);


public sealed class Participant
{
    [Column(TypeName = "varchar(36)")]
    public ParticipantId Id { get; set; }
    
    public string Name { get; set; }

    public int Rank { get; set; } = 3;

    public static Participant CreateBye()
    {
        return new() { 
            Id = new ParticipantId("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
            Name = "Bye",
            Rank = int.MaxValue
        };
    }
}