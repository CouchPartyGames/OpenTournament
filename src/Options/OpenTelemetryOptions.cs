namespace OpenTournament.Options;

public sealed class OpenTelemetryOptions
{
   public const string SectionName = "OpenTelemetry";

   public bool Enabled { get; init; } = false;

   public string Address { get; init; } = "http://localhost:4317";
}