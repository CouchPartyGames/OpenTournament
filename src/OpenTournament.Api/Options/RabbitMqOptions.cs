namespace OpenTournament.Api.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    
    public string Host { get; set; } = "localhost";
    
    public string UserName { get; set; } = "guest";
    
    public string Password { get; set; } = "guest";
}