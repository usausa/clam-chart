namespace ClamChart.Rendering;

using System.Runtime.InteropServices;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal static class ThresholdSteps
{
    public static void Resolve(IList<ChartThreshold> thresholds, SKColor baseColor, ChartStyle style, List<ChartDrawingThreshold> steps)
    {
        steps.Clear();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < thresholds.Count; i++)
        {
            var threshold = thresholds[i];
            steps.Add(new ChartDrawingThreshold(threshold.Value, ThresholdResolver.GetColor(threshold, style)?.ToSKColor() ?? baseColor));
        }

        Sort(CollectionsMarshal.AsSpan(steps));
    }

    public static void Sort(Span<ChartDrawingThreshold> steps)
    {
        for (var i = 1; i < steps.Length; i++)
        {
            var item = steps[i];
            var j = i - 1;
            while ((j >= 0) && (steps[j].Value > item.Value))
            {
                steps[j + 1] = steps[j];
                j--;
            }

            steps[j + 1] = item;
        }
    }

    public static SKColor GetColor(ReadOnlySpan<ChartDrawingThreshold> steps, double percent, SKColor baseColor)
    {
        if (!Double.IsFinite(percent))
        {
            return baseColor;
        }

        var found = -1;
        for (var i = 0; i < steps.Length; i++)
        {
            if ((steps[i].Value <= percent) && ((found < 0) || (steps[i].Value >= steps[found].Value)))
            {
                found = i;
            }
        }

        return found >= 0 ? steps[found].Color : baseColor;
    }
}
