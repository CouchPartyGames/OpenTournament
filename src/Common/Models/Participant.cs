using System.ComponentModel.DataAnnotations.Schema;

namespace OpenTournament.Common.Models;

public sealed record ParticipantId(Guid Value)
{
    
    public static ParticipantId TryParse(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return null;
        }
        return new ParticipantId(guid);
    }
    public static ParticipantId Create() => new ParticipantId(Guid.NewGuid());
}

public sealed class Participant
{
    [Column(TypeName = "varchar(36)")]
    public ParticipantId Id { get; set; }
    
    public string Name { get; set; }
}