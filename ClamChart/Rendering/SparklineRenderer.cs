namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class SparklineRenderer : IDisposable
{
    private const float WinLossGap = 1;

    private const float BarGapRatio = 0.25f;

    private const float ReferenceDash = 2;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly SKPathEffect dash = SKPathEffect.CreateDash([ReferenceDash, ReferenceDash], 0);
    private float[] along = [];
    private float[] across = [];
    private float[] tangents = [];

    public IChartTextMeasurer Measurer => text;

    public SparklineRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
        dash.Dispose();
        Return(ref along);
        Return(ref across);
        Return(ref tangents);
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, SKColor color, SKColor negative, ChartStyle style, bool smooth)
    {
        var scale = display.IsMorphing ? display.Scale : layout.Scale;
        var entrance = display.Entrance;
        var alpha = entrance == ChartEntranceAnimation.Fade ? display.Ease(display.Progress) : 1;
        var overlay = display.OverlayAlpha;
        DrawReference(canvas, layout, scale, style, overlay);
        var count = 0;
        canvas.Save();
        try
        {
            ClipPlot(canvas, layout, display, entrance);
            if (layout.IsBar)
            {
                DrawBars(canvas, layout, display, values, scale, color, negative, style, alpha, entrance);
            }
            else
            {
                count = Project(layout, display, values, scale, entrance);
                if (layout.Kind == ChartSparklineKind.Area)
                {
                    var fill = entrance == ChartEntranceAnimation.Reveal ? alpha * display.Ease(ChartProgress.Delayed(display.Progress, ChartProgress.FillDelay)) : alpha;
                    FillArea(canvas, layout, count, smooth, ChartColor.Fade(color, fill * style.AreaFillOpacity));
                }

                StrokeLine(canvas, count, smooth, ChartColor.Fade(color, alpha), layout.Thickness);
            }
        }
        finally
        {
            canvas.Restore();
        }

        if (!layout.IsBar)
        {
            DrawMarkers(canvas, layout, display, count, ChartColor.Fade(color, alpha * overlay));
        }
    }

    private static void Ensure(ref float[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            Return(ref buffer);
            buffer = DataSnapshot.Rent<float>(count);
        }
    }

    private static void Return(ref float[] buffer)
    {
        DataSnapshot.Return(buffer);
        buffer = [];
    }

    private static double Grow(double value, double origin, double factor) => origin + ((value - origin) * factor);

    private static double GetGrowth(SparklineInterpolator display, ChartEntranceAnimation entrance, int index, int count) =>
        entrance == ChartEntranceAnimation.Grow ? display.Ease(ChartProgress.ForElement(display.Progress, index, count)) : 1;

    private static float Snap(double value, float scale) => (MathF.Floor((float)value * scale) + 0.5f) / scale;

    private static void ClipPlot(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, ChartEntranceAnimation entrance)
    {
        var bounds = layout.Bounds;
        var plot = layout.Plot;
        var margin = layout.IsBar ? 0 : layout.Thickness / 2;
        var left = display.IsMorphing ? (float)plot.X - margin : (float)bounds.X;
        var right = display.IsMorphing ? (float)plot.Right + margin : (float)bounds.Right;
        if (entrance == ChartEntranceAnimation.Reveal)
        {
            right = (float)(plot.X + (plot.Width * display.Ease(display.Progress))) + margin;
        }

        canvas.ClipRect(new SKRect(left, (float)bounds.Y, right, (float)bounds.Bottom));
    }

    private void DrawReference(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, ChartStyle style, double overlay)
    {
        if ((layout.Reference is not { } reference) || (overlay <= 0) || (reference < scale.Minimum) || (reference > scale.Maximum))
        {
            return;
        }

        var plot = layout.Plot;
        var y = Snap(layout.ToY(reference, scale), canvas.TotalMatrix.ScaleY);
        var paint = paints.Stroke(ChartColor.Fade(style.AxisLineColor.ToSKColor(), overlay), 0, SKStrokeCap.Butt);
        paint.PathEffect = dash;
        canvas.DrawLine((float)plot.X, y, (float)plot.Right, y, paint);
        paint.PathEffect = null;
    }

    private int Project(SparklineLayout layout, SparklineInterpolator display, SparklineValues values, SparklineScale scale, ChartEntranceAnimation entrance)
    {
        var morphing = display.IsMorphing;
        var geometry = display.Display;
        var count = morphing ? geometry.Count : values.Count;
        Ensure(ref along, count);
        Ensure(ref across, count);
        Ensure(ref tangents, count);
        var origin = SparklineLayout.GetBase(scale);
        for (var i = 0; i < count; i++)
        {
            var position = morphing ? geometry.X[i] : i;
            var value = morphing ? geometry.End[i] : values[i];
            along[i] = Double.IsFinite(position) ? (float)layout.ToX(position, scale) : Single.NaN;
            across[i] = Double.IsFinite(value) ? (float)layout.ToY(Grow(value, origin, GetGrowth(display, entrance, i, count)), scale) : Single.NaN;
        }

        return count;
    }

    private void StrokeLine(SKCanvas canvas, int count, bool smooth, SKColor color, float thickness)
    {
        if (thickness <= 0)
        {
            return;
        }

        var builder = paints.Path;
        var interpolation = smooth ? ChartLineInterpolation.Smooth : ChartLineInterpolation.Linear;
        var drawn = false;
        var first = -1;
        var last = -1;
        for (var start = 0; start < count;)
        {
            if (!IsPoint(start))
            {
                start++;
                continue;
            }

            var end = start;
            while ((end < count) && IsPoint(end))
            {
                end++;
            }

            var runAlong = along.AsSpan(start, end - start);
            var runAcross = across.AsSpan(start, end - start);
            var runTangents = tangents.AsSpan(start, end - start);
            if (smooth)
            {
                LinePathBuilder.ComputeTangents(runAlong, runAcross, runTangents);
            }

            LinePathBuilder.AddLine(builder, runAlong, runAcross, runTangents, interpolation, false);
            drawn = true;
            first = first < 0 ? start : first;
            last = end - 1;
            start = end;
        }

        if (!drawn)
        {
            return;
        }

        using var path = builder.Detach();
        var spacing = last > first ? Math.Abs(along[last] - along[first]) / (last - first) : Single.PositiveInfinity;
        canvas.DrawPath(path, paints.Stroke(color, thickness, SKStrokeCap.Round, spacing < thickness ? SKStrokeJoin.Bevel : SKStrokeJoin.Round));

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void FillArea(SKCanvas canvas, SparklineLayout layout, int count, bool smooth, SKColor color)
    {
        if (color.Alpha == 0)
        {
            return;
        }

        var builder = paints.Path;
        var interpolation = smooth ? ChartLineInterpolation.Smooth : ChartLineInterpolation.Linear;
        var bottom = (float)layout.Plot.Bottom;
        var drawn = false;
        for (var start = 0; start < count;)
        {
            if (!IsPoint(start))
            {
                start++;
                continue;
            }

            var end = start;
            while ((end < count) && IsPoint(end))
            {
                end++;
            }

            if (end - start >= 2)
            {
                var runAlong = along.AsSpan(start, end - start);
                var runAcross = across.AsSpan(start, end - start);
                var runTangents = tangents.AsSpan(start, end - start);
                if (smooth)
                {
                    LinePathBuilder.ComputeTangents(runAlong, runAcross, runTangents);
                }

                LinePathBuilder.AddLine(builder, runAlong, runAcross, runTangents, interpolation, false);
                builder.LineTo(runAlong[^1], bottom);
                builder.LineTo(runAlong[0], bottom);
                builder.Close();
                drawn = true;
            }

            start = end;
        }

        if (!drawn)
        {
            return;
        }

        using var path = builder.Detach();
        canvas.DrawPath(path, paints.Fill(color));

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void DrawBars(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, SparklineScale scale, SKColor color, SKColor negative, ChartStyle style, double alpha, ChartEntranceAnimation entrance)
    {
        var morphing = display.IsMorphing;
        var geometry = display.Display;
        var count = morphing ? geometry.Count : values.Count;
        var slot = layout.GetSlot(scale);
        var gap = Math.Min(style.SegmentGap, slot * BarGapRatio);
        var width = (float)Math.Max(0, Math.Min(slot - gap, style.MaxBarWidth));
        if (width <= 0)
        {
            return;
        }

        var plot = layout.Plot;
        var winLoss = layout.Kind == ChartSparklineKind.WinLoss;
        var origin = SparklineLayout.GetBase(scale);
        var middle = (float)layout.ToY(origin, scale);
        for (var i = 0; i < count; i++)
        {
            var position = morphing ? geometry.X[i] : i;
            var value = morphing ? geometry.End[i] : values[i];
            if (!Double.IsFinite(position) || !Double.IsFinite(value) || (value == 0))
            {
                continue;
            }

            var growth = GetGrowth(display, entrance, i, count);
            var center = (float)layout.ToX(position, scale);
            float top;
            float bottom;
            if (winLoss)
            {
                var extent = (float)(plot.Height / 2 * growth);
                top = value > 0 ? middle - extent : middle + (WinLossGap / 2);
                bottom = value > 0 ? middle - (WinLossGap / 2) : middle + extent;
            }
            else
            {
                var end = (float)layout.ToY(Grow(Math.Clamp(value, scale.Minimum, scale.Maximum), origin, growth), scale);
                top = Math.Min(middle, end);
                bottom = Math.Max(middle, end);
            }

            if (bottom - top <= 0)
            {
                continue;
            }

            canvas.DrawRect(new SKRect(center - (width / 2), top, center + (width / 2), bottom), paints.Fill(ChartColor.Fade(value < 0 ? negative : color, alpha)));
        }
    }

    private void DrawMarkers(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, int count, SKColor color)
    {
        if ((layout.Markers == ChartSparklineMarkers.None) || (color.Alpha == 0))
        {
            return;
        }

        var markers = layout.Markers;
        var first = (markers & ChartSparklineMarkers.First) != 0 ? layout.FirstIndex : -1;
        var last = (markers & ChartSparklineMarkers.Last) != 0 ? layout.LastIndex : -1;
        var high = (markers & ChartSparklineMarkers.High) != 0 ? layout.HighIndex : -1;
        var low = (markers & ChartSparklineMarkers.Low) != 0 ? layout.LowIndex : -1;
        DrawMarker(canvas, layout, display, count, first, color);
        if (last != first)
        {
            DrawMarker(canvas, layout, display, count, last, color);
        }

        if ((high != first) && (high != last))
        {
            DrawMarker(canvas, layout, display, count, high, color);
        }

        if ((low != first) && (low != last) && (low != high))
        {
            DrawMarker(canvas, layout, display, count, low, color);
        }
    }

    private void DrawMarker(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, int count, int index, SKColor color)
    {
        var position = index >= 0 ? display.ToDisplayIndex(index) : -1;
        if ((position < 0) || (position >= count) || !Single.IsFinite(along[position]) || !Single.IsFinite(across[position]))
        {
            return;
        }

        var point = new SKPoint(along[position], across[position]);
        if (layout.MarkerKnockout > 0)
        {
            canvas.DrawCircle(point, layout.MarkerRadius + layout.MarkerKnockout, paints.Clear);
        }

        canvas.DrawCircle(point, layout.MarkerRadius, paints.Fill(color));
    }
}
