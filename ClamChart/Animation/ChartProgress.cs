namespace ClamChart.Animation;

internal static class ChartProgress
{
    public const double Stagger = 0.3;

    public const double OverlayDelay = 0.7;

    public const double FillDelay = 0.3;

    public static double ForElement(double progress, int index, int count) => ForElement(progress, index, count, Stagger);

    public static double ForElement(double progress, int index, int count, double stagger)
    {
        var span = 1 - stagger;
        if (count <= 1)
        {
            return Math.Clamp(progress / span, 0, 1);
        }

        var start = stagger * index / (count - 1);
        return Math.Clamp((progress - start) / span, 0, 1);
    }

    public static double GetStagger(int count, double delay, double length) =>
        (count > 1) && (length > 0) ? Math.Min(Stagger, delay * (count - 1) / length) : 0;

    public static double Delayed(double progress, double delay) => Math.Clamp((progress - delay) / (1 - delay), 0, 1);

    public static double Lerp(double from, double to, double progress) => from + ((to - from) * progress);
}
