using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Data.Models;

public sealed record ParticipantId(string Value);

public sealed class Participant
{
    [Column(TypeName = "varchar(36)")]
    public ParticipantId Id { get; set; }
    
    public string Name { get; set; }
}