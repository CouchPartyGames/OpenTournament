namespace OpenTournament.Core.Infrastructure;

public sealed class PostgresOptions
{
    public const string SectionName = "Database";

    public string Type { get; set; } = String.Empty;
    public string ConnectionString { get; set; } = String.Empty;
}
