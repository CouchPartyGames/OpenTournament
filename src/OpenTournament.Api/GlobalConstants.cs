using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api;

public static class GlobalConstants
{
    public const string ServiceName = "OpenTournament";

    public const string ServiceVersion = "0.1.0";
    
    public const string HealthPageUri = "/health";
    
    public static readonly Participant ByeOpponent = Participant.CreateBye();
    
    public const string ProdCorsPolicyName = "prod";
    public const string DevCorsPolicyName = "dev";
}