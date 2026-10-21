namespace ClamChart.Rendering;

using SkiaSharp;

internal static class LiveIndicator
{
    public const double Milliseconds = 1800;

    public const float ExtentRatio = 1 + RingSpread;

    private const int RingCount = 2;

    private const float RingSpread = 2;

    private const float RingWidthRatio = 0.5f;

    private const double RingOpacity = 0.45;

    public static void DrawRings(SKCanvas canvas, PaintCache paints, SKPoint center, float radius, SKColor color, double phase)
    {
        if (!Double.IsFinite(phase) || (radius <= 0))
        {
            return;
        }

        var width = radius * RingWidthRatio;
        for (var i = 0; i < RingCount; i++)
        {
            var progress = (phase + ((double)i / RingCount)) % 1;
            canvas.DrawCircle(center, radius * (1 + (RingSpread * (float)progress)), paints.Stroke(ChartColor.Fade(color, RingOpacity * (1 - progress)), width));
        }
    }
}
