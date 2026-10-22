namespace ClamChart.Rendering;

internal static class ChartGlow
{
    public const float WidthRatio = 1.8f;

    public const double Opacity = 0.55;

    public static float GetSigma(double radius) => (float)(radius / 2);

    public static double GetExtent(double thickness, double radius) => (thickness * (WidthRatio - 1) / 2) + radius;
}
