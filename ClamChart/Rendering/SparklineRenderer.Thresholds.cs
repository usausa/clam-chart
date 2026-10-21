namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed partial class SparklineRenderer
{
    private const float ThresholdLineWidth = 1;

    private const float ThresholdDash = 4;

    private const double ThresholdLineOpacity = 0.7;

    private const double BandOpacity = 0.15;

    private readonly SKPathEffect thresholdDash = SKPathEffect.CreateDash([ThresholdDash, ThresholdDash], 0);

    private static SKColor GetThresholdColor(SparklineLayout layout, ChartStyle style, double value, SKColor color) =>
        ThresholdResolver.GetColor(ThresholdResolver.ResolveSorted(layout.Thresholds, value), style)?.ToSKColor() ?? color;

    private static SKRect GetSpanRect(SparklineLayout layout, SparklineScale scale, double from, double to, float inflate)
    {
        var bounds = layout.Bounds;
        var low = (float)Math.Clamp(layout.ToY(Math.Max(from, scale.Minimum - (scale.Maximum - scale.Minimum)), scale), bounds.Y - inflate, bounds.Bottom + inflate);
        var high = (float)Math.Clamp(layout.ToY(Math.Min(to, scale.Maximum + (scale.Maximum - scale.Minimum)), scale), bounds.Y - inflate, bounds.Bottom + inflate);
        return new SKRect((float)bounds.X - inflate, Math.Min(low, high), (float)bounds.Right + inflate, Math.Max(low, high));
    }

    private static void DrawRegions(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, SKPath path, SKPaint paint, SKColor color, double alpha, ChartStyle style, float inflate)
    {
        var thresholds = layout.Thresholds;
        var first = Double.IsNegativeInfinity(thresholds[0].Value) ? 1 : 0;
        var regionColor = first == 1 ? ThresholdResolver.GetColor(thresholds[0], style)?.ToSKColor() ?? color : color;
        var start = Double.NegativeInfinity;
        for (var i = first; i <= thresholds.Count; i++)
        {
            var end = i < thresholds.Count ? thresholds[i].Value : Double.PositiveInfinity;
            var rect = GetSpanRect(layout, scale, start, end, inflate);
            if ((rect.Width > 0) && (rect.Height > 0))
            {
                canvas.Save();
                canvas.ClipRect(rect);
                paint.Color = ChartColor.Fade(regionColor, alpha);
                canvas.DrawPath(path, paint);
                canvas.Restore();
            }

            if (i < thresholds.Count)
            {
                start = end;
                regionColor = ThresholdResolver.GetColor(thresholds[i], style)?.ToSKColor() ?? color;
            }
        }
    }

    private void DrawBackground(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, ChartStyle style, double overlay, ChartThresholdsMode mode)
    {
        if (overlay <= 0)
        {
            return;
        }

        var plot = layout.Plot;
        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)plot.X, (float)plot.Y, (float)plot.Width, (float)plot.Height));
        if (layout.HasNormalRange)
        {
            var low = Double.IsNaN(layout.NormalLow) ? Double.NegativeInfinity : layout.NormalLow;
            var high = Double.IsNaN(layout.NormalHigh) ? Double.PositiveInfinity : layout.NormalHigh;
            canvas.DrawRect(GetSpanRect(layout, scale, Math.Min(low, high), Math.Max(low, high), 0), paints.Fill(ChartColor.Fade(style.MutedTextColor.ToSKColor(), BandOpacity * overlay)));
        }

        var thresholds = layout.Thresholds;
        if ((mode & ChartThresholdsMode.Bands) != 0)
        {
            for (var i = 0; i < thresholds.Count; i++)
            {
                if (ThresholdResolver.GetColor(thresholds[i], style) is not { } color)
                {
                    continue;
                }

                var to = i + 1 < thresholds.Count ? thresholds[i + 1].Value : Double.PositiveInfinity;
                canvas.DrawRect(GetSpanRect(layout, scale, thresholds[i].Value, to, 0), paints.Fill(ChartColor.Fade(color.ToSKColor(), BandOpacity * overlay)));
            }
        }

        canvas.Restore();
        if ((mode & ChartThresholdsMode.Lines) == 0)
        {
            return;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < thresholds.Count; i++)
        {
            var threshold = thresholds[i];
            if (!Double.IsFinite(threshold.Value) || (threshold.Value < scale.Minimum) || (threshold.Value > scale.Maximum) || (ThresholdResolver.GetColor(threshold, style) is not { } color))
            {
                continue;
            }

            var y = (float)layout.ToY(threshold.Value, scale);
            var paint = paints.Stroke(ChartColor.Fade(color.ToSKColor(), ThresholdLineOpacity * overlay), ThresholdLineWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter);
            paint.PathEffect = thresholdDash;
            canvas.DrawLine((float)plot.X, y, (float)plot.Right, y, paint);
            paint.PathEffect = null;
        }
    }
}
