namespace ClamChart.Rendering;

using SkiaSharp;

internal static class RadialBarGradient
{
    private const float Spin = 20;

    private const float DarkDarken = 15;

    private const float LightDarken = 5;

    private const float Saturate = 20;

    private const int Brighten = 10;

    private const float Lighten = 10;

    private const int StackLimit = 32;

    public static (SKColor[] Colors, float[] Positions) GetThresholdStops(ReadOnlySpan<ChartDrawingThreshold> sorted, SKColor baseColor)
    {
        var capacity = sorted.Length + 2;
        var colors = capacity <= StackLimit ? stackalloc SKColor[capacity] : new SKColor[capacity];
        var positions = capacity <= StackLimit ? stackalloc float[capacity] : new float[capacity];
        var count = 0;
        if ((sorted.Length == 0) || !Double.IsNegativeInfinity(sorted[0].Value))
        {
            colors[count] = baseColor;
            positions[count++] = 0;
        }

        foreach (var threshold in sorted)
        {
            if (threshold.Value > 100)
            {
                break;
            }

            colors[count] = threshold.Color;
            positions[count++] = Double.IsNegativeInfinity(threshold.Value) ? 0 : (float)Math.Clamp(threshold.Value / 100, 0, 1);
        }

        if (positions[count - 1] < 1)
        {
            colors[count] = colors[count - 1];
            positions[count++] = 1;
        }

        return (colors[..count].ToArray(), positions[..count].ToArray());
    }

    public static (SKColor[] Colors, float[] Positions) GetColorStops(SKColor color, double ratio, bool dark) =>
        GetColorStops(color, ratio, dark ? -DarkDarken : LightDarken);

    public static (SKColor[] Colors, float[] Positions) GetColorStops(SKColor color, double ratio, double shift)
    {
        var dark = shift < 0;
        var darker = Shift(color, -Spin, 0, -(float)Math.Abs(shift));
        var lighter = Shift(AddBrightness(Shift(color, Spin, Saturate, 0), Brighten), 0, 0, Lighten);
        var value = (float)Math.Clamp(ratio, 0, 1);
        var start = dark ? ChartColor.Lerp(darker, lighter, 1 - value) : lighter;
        var end = dark ? lighter : ChartColor.Lerp(lighter, darker, value);
        return ([start, end, end], [0, value, 1]);
    }

    public static SKColor GetColorAt(ReadOnlySpan<SKColor> colors, ReadOnlySpan<float> positions, double position)
    {
        if (position <= positions[0])
        {
            return colors[0];
        }

        for (var i = 1; i < positions.Length; i++)
        {
            if (position <= positions[i])
            {
                var range = positions[i] - positions[i - 1];
                return range <= 0 ? colors[i] : ChartColor.Lerp(colors[i - 1], colors[i], (position - positions[i - 1]) / range);
            }
        }

        return colors[^1];
    }

    private static SKColor Shift(SKColor color, float hue, float saturation, float lightness)
    {
        color.ToHsl(out var h, out var s, out var l);
        return SKColor.FromHsl((((h + hue) % 360) + 360) % 360, Math.Clamp(s + saturation, 0, 100), Math.Clamp(l + lightness, 0, 100), color.Alpha);
    }

    private static SKColor AddBrightness(SKColor color, int amount)
    {
        var delta = (int)Math.Round(255 * amount / 100d);
        return new SKColor((byte)Math.Min(255, color.Red + delta), (byte)Math.Min(255, color.Green + delta), (byte)Math.Min(255, color.Blue + delta), color.Alpha);
    }
}
