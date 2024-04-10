using System.Diagnostics;

namespace OpenTournament.Common;

public static class Telemetry
{
    const string AppName = "OpenTournament";
    
    const string AppVersion = "1.0.0";
    
    public static readonly ActivitySource ActivitySource = new(AppName, AppVersion);
}