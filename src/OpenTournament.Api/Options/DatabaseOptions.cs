namespace OpenTournament.Api.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Type { get; set; } = String.Empty;
    public string ConnectionString { get; set; } = String.Empty;
}

/*
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    
    public int Port { get; init; }
    public string Host { get; init; }
    public string Databae { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }

    public string ConnectionString
    {
        get
        {
            return $"Server={Host};Port={Port};Database={Database};User Id={Username};Password={Password}";
        };
    }
}
*/
