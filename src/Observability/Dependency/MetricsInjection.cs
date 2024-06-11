using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTournament.Observability.Options;

namespace OpenTournament.Observability.Dependency;


public static class MetricsInjection
{
    public static IServiceCollection AddObservabilityMetrics(this IServiceCollection services,
        IConfiguration configuration,
        ResourceBuilder resourceBuilder)
    {
        var options = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>();
        
        services.AddOpenTelemetry()
            .WithMetrics(metricBuilder =>
            {
                metricBuilder.SetResourceBuilder(resourceBuilder);
                metricBuilder
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
                metricBuilder.AddOtlpExporter(export =>
                {
                    export.Endpoint = new Uri(OpenTelemetryOptions.OtelDefaultEndpoint);
                    export.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        
        
        services.AddSingleton<OpenTournamentMetrics>();
        
        return services;
    }
    
}