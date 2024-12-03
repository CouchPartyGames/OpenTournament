namespace OpenTournament.Api.Observability;

public sealed class OpenTelemetryOptions
{
   public const string SectionName = "OpenTelemetry";

   public const string OtelDefaultEndpoint = "http://localhost:4317";

   public bool Enabled { get; init; } = false;

   public string Address { get; init; } = OtelDefaultEndpoint;
}