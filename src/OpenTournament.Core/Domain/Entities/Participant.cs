using System.ComponentModel.DataAnnotations.Schema;
using CouchPartyGames.TournamentGenerator;

namespace OpenTournament.Api.Data.Models;

public sealed class Participant : IOpponent, IEquatable<Participant>
{
    [Column(TypeName = "varchar(36)")]
    public required ParticipantId Id { get; init; }
    
    [Column(TypeName = "varchar(50)")]
    public required string Name { get; init; }

    public int Rank { get; init; } = int.MaxValue - 1;

    public static Participant CreateBye()
    {
        return new() { 
            Id = new ParticipantId("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
            Name = "Bye",
            Rank = int.MaxValue
        };
    }

    public bool Equals(Participant? other)
    {
        if (other is null) return false;

        return other.Id == Id;
    }
}