using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core.Features.Tournaments.Create;

public sealed record CreateTournamentCommand(
    [Required]
    [property: Description("name of the tournament")]	
    string Name,
    [property: Description("start time of the tournament")]
    DateTime StartTime,
    [property: Description("minimum number of users required to start a tournament")]
    int MinParticipants,
    [property: Description("maximum number of users allowed to join")]
    int MaxParticipants,
    [property: Description("single or double elimination tournament")]
    EliminationMode Mode,
    [property: Description("seed players randomly or by rank")]
    DrawSeeding Seeding);
