namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal static class BarGaugeGradient
{
    public static void GetStops(IList<ChartThreshold> thresholds, double percent, double ratio, Color baseColor, ChartStyle style, List<SKColor> colors, List<float> positions)
    {
        colors.Clear();
        positions.Clear();
        var sorted = Sort(thresholds);
        var first = (sorted.Length > 0) && Double.IsNegativeInfinity(sorted[0].Value) ? 1 : 0;
        var pending = (first == 1 ? ThresholdResolver.GetColor(sorted[0], style) : null) ?? baseColor;
        colors.Add(pending.ToSKColor());
        positions.Add(0);
        var last = 0d;
        for (var i = first; i < sorted.Length; i++)
        {
            var threshold = sorted[i];
            if (Double.IsNaN(percent) || (percent < threshold.Value))
            {
                break;
            }

            var position = Math.Clamp(threshold.Value / 100, 0, 1);
            colors.Add(pending.ToSKColor());
            positions.Add((float)Math.Min(ratio, position - ((position - last) / 2)));
            pending = ThresholdResolver.GetColor(threshold, style) ?? baseColor;
            last = position;
        }

        colors.Add(pending.ToSKColor());
        positions.Add((float)Math.Max(ratio, positions[^1]));
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
