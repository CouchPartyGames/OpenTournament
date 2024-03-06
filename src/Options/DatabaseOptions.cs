namespace OpenTournament.Options;

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
    public int Host { get; init; }
    public int Username { get; init; }
    public int Password { get; init; }

    public string ConnectionString
    {
        get
        {
            return $"Server={Host};Port={Port};User Id={Username};Password={Password}";
        };
    }
}
*/
