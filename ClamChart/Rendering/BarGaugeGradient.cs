namespace ClamChart.Rendering;

using SkiaSharp;

internal static class BarGaugeGradient
{
    private const int StackLimit = 32;

    public static (SKColor[] Colors, float[] Positions) GetStops(ReadOnlySpan<ChartDrawingThreshold> sorted, double percent, double ratio, SKColor baseColor)
    {
        var capacity = sorted.Length + 2;
        var colors = capacity <= StackLimit ? stackalloc SKColor[capacity] : new SKColor[capacity];
        var positions = capacity <= StackLimit ? stackalloc float[capacity] : new float[capacity];
        var first = (sorted.Length > 0) && Double.IsNegativeInfinity(sorted[0].Value) ? 1 : 0;
        var pending = first == 1 ? sorted[0].Color : baseColor;
        colors[0] = pending;
        positions[0] = 0;
        var count = 1;
        var last = 0d;
        for (var i = first; i < sorted.Length; i++)
        {
            var threshold = sorted[i];
            if (Double.IsNaN(percent) || (percent < threshold.Value))
            {
                break;
            }

            var position = Math.Clamp(threshold.Value / 100, 0, 1);
            colors[count] = pending;
            positions[count++] = (float)Math.Min(ratio, position - ((position - last) / 2));
            pending = threshold.Color;
            last = position;
        }

        colors[count] = pending;
        positions[count] = (float)Math.Max(ratio, positions[count - 1]);
        count++;
        return (colors[..count].ToArray(), positions[..count].ToArray());
    }
}
