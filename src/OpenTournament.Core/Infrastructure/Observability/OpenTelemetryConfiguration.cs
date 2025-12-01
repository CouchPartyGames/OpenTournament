using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTournament.Core.Infrastructure;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenTelemetryOptions>()
            .BindConfiguration(OpenTelemetryOptions.SectionName)
            .ValidateDataAnnotations();
        /*
        var options = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>();
            */
        
        var endpoint = new Uri(OpenTelemetryOptions.OtelDefaultEndpoint);
        var protocol = OtlpExportProtocol.Grpc;
        
        services.AddOpenTelemetry()
            .ConfigureResource(config => 
            {
                config.AddService(GlobalConstants.ServiceName, null, GlobalConstants.ServiceVersion); 
            })
            .WithLogging(logging => { }, loggerOptions =>
            {
                loggerOptions.IncludeScopes = true;
                loggerOptions.IncludeFormattedMessage = true;
                loggerOptions.IncludeScopes = true;
            })
            .WithMetrics(metricBuilder =>
            {
                metricBuilder
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
            })
            .WithTracing(traceBuilder =>
            {
                traceBuilder.SetSampler(new TraceIdRatioBasedSampler(1.0));
                traceBuilder.AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
            })
            .UseOtlpExporter(protocol, endpoint);
        return services;
    }
}