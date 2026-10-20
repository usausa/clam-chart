namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal static class RadialBarGradient
{
    private const float Spin = 20;

    private const float DarkDarken = 15;

    private const float LightDarken = 5;

    private const float Saturate = 20;

    private const int Brighten = 10;

    private const float Lighten = 10;

    public static void GetThresholdStops(IList<ChartThreshold> thresholds, SKColor baseColor, ChartStyle style, List<SKColor> colors, List<float> positions)
    {
        colors.Clear();
        positions.Clear();
        var sorted = Sort(thresholds);
        if ((sorted.Length == 0) || !Double.IsNegativeInfinity(sorted[0].Value))
        {
            colors.Add(baseColor);
            positions.Add(0);
        }

        foreach (var threshold in sorted)
        {
            if (threshold.Value > 100)
            {
                break;
            }

            colors.Add(ThresholdResolver.GetColor(threshold, style)?.ToSKColor() ?? baseColor);
            positions.Add(Double.IsNegativeInfinity(threshold.Value) ? 0 : (float)Math.Clamp(threshold.Value / 100, 0, 1));
        }

        if (positions[^1] < 1)
        {
            colors.Add(colors[^1]);
            positions.Add(1);
        }
    }

    public static void GetColorStops(SKColor color, double ratio, bool dark, List<SKColor> colors, List<float> positions)
    {
        colors.Clear();
        positions.Clear();
        var darker = Shift(color, -Spin, 0, dark ? -DarkDarken : -LightDarken);
        var lighter = Shift(AddBrightness(Shift(color, Spin, Saturate, 0), Brighten), 0, 0, Lighten);
        var value = (float)Math.Clamp(ratio, 0, 1);
        var start = dark ? ChartColor.Lerp(darker, lighter, 1 - value) : lighter;
        var end = dark ? lighter : ChartColor.Lerp(lighter, darker, value);
        colors.Add(start);
        positions.Add(0);
        colors.Add(end);
        positions.Add(value);
        colors.Add(end);
        positions.Add(1);
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

    private static ChartThreshold[] Sort(IList<ChartThreshold> thresholds)
    {
        var sorted = new ChartThreshold[thresholds.Count];
        thresholds.CopyTo(sorted, 0);
        for (var i = 1; i < sorted.Length; i++)
        {
            var item = sorted[i];
            var j = i - 1;
            while ((j >= 0) && (sorted[j].Value > item.Value))
            {
                sorted[j + 1] = sorted[j];
                j--;
            }

            sorted[j + 1] = item;
        }

        return sorted;
    }
}
