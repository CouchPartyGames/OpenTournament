using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core.Features.Tournaments.Update;

public sealed record UpdateTournamentCommand(
    string Name,
    DateTime StartTime,
    int MinParticipants,
    int MaxParticipants,
    EliminationMode EliminationMode,
    DrawSeeding Seeding);
