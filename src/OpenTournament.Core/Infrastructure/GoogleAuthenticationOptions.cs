namespace OpenTournament.Core.Infrastructure;

public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "Google";
    
    public string ClientId { get; set; }
    
    public string ClientSecret { get; set; }
    
}