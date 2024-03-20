using System.Diagnostics;

namespace OpenTournament.Common;

public static class Telemetry
{
    const string AppName = "OpenTournament";
    
    public static readonly ActivitySource ActivitySource = new(AppName, "1.0.0");
}