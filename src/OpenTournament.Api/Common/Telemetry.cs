using System.Diagnostics;
using OpenTelemetry.Resources;

namespace OpenTournament.Api.Common;

public static class Telemetry
{
    const string AppName = "OpenTournament";
    
    const string AppVersion = "1.0.0";
    
    public static readonly ActivitySource ActivitySource = new(AppName, AppVersion);
    
    public static readonly ResourceBuilder ResourceBuilder = ResourceBuilder
        .CreateDefault()
        .AddService(AppName);
}