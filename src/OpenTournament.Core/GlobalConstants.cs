using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core;

public static class GlobalConstants
{
    public const string ServiceName = "OpenTournament";

    public const string ServiceVersion = "0.1.0";
    
    public const string HealthPageUri = "/health";
    
    public static readonly Participant ByeOpponent = Participant.CreateBye();
}