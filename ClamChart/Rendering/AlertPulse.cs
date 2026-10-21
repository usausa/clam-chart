namespace ClamChart.Rendering;

internal static class AlertPulse
{
    public const double Milliseconds = 1200;

    private const double MinimumOpacity = 0.6;

    public static double GetOpacity(double phase) =>
        Double.IsFinite(phase) ? 1 - ((1 - MinimumOpacity) * (0.5 - (0.5 * Math.Cos(phase * 2 * Math.PI)))) : 1;
}
