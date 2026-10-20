namespace ClamChart.Rendering;

using System.Diagnostics;

internal static class ChartDiagnostics
{
    public static void Warn(string message) => Trace.TraceWarning("ClamChart: " + message);
}
