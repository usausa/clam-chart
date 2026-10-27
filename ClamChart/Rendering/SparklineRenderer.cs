namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed partial class SparklineRenderer : IDisposable
{
    private const float MaxBarWidth = 24;

    private const float WinLossGap = 1;

    private const float BarGapRatio = 0.25f;

    private const float ReferenceDash = 2;

    private readonly PaintCache paints = new();
    private readonly GradientEntry areaGradient = new();
    private readonly ChartTextPainter text;
    private readonly bool ownsText;
    private readonly SKPathEffect dash = SKPathEffect.CreateDash([ReferenceDash, ReferenceDash], 0);
    private float[] along = [];
    private float[] across = [];
    private float[] tangents = [];

    public IChartTextMeasurer Measurer => text;

    public SparklineRenderer(string fontFamily)
        : this(new ChartTextPainter(fontFamily), true)
    {
    }

    public SparklineRenderer(ChartTextPainter text)
        : this(text, false)
    {
    }

    private SparklineRenderer(ChartTextPainter text, bool ownsText)
    {
        this.text = text;
        this.ownsText = ownsText;
    }

    public void Dispose()
    {
        paints.Dispose();
        if (ownsText)
        {
            text.Dispose();
        }

        areaGradient.Shader?.Dispose();
        dash.Dispose();
        thresholdDash.Dispose();
        Return(ref along);
        Return(ref across);
        Return(ref tangents);
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, SparklinePaint paint, ChartStyle style, bool smooth, ChartThresholdsMode thresholdsMode = ChartThresholdsMode.None, bool live = false, double livePhase = Double.NaN)
    {
        var scale = display.IsMorphing ? display.Scale : layout.Scale;
        var entrance = display.Entrance;
        var alpha = entrance == ChartEntranceAnimation.Fade ? display.Ease(display.Progress) : 1;
        var overlay = display.OverlayAlpha;
        DrawBackground(canvas, layout, scale, style, overlay, thresholdsMode);
        DrawReference(canvas, layout, scale, style, overlay);
        var coloring = ((thresholdsMode & ChartThresholdsMode.ColorSeries) != 0) && (layout.Thresholds.Count > 0);
        var count = 0;
        canvas.Save();
        try
        {
            ClipPlot(canvas, layout, display, entrance);
            if (layout.IsBar)
            {
                DrawBars(canvas, layout, display, values, scale, paint, style, alpha, entrance, coloring);
            }
            else
            {
                count = Project(layout, display, values, scale, entrance);
                if (layout.Kind == ChartSparklineKind.Area)
                {
                    var fill = entrance == ChartEntranceAnimation.Reveal ? alpha * display.Ease(ChartProgress.Delayed(display.Progress, ChartProgress.FillDelay)) : alpha;
                    var weak = paint.HasEmphasis ? paint.WeakOpacity : 1;
                    if (coloring)
                    {
                        FillArea(canvas, layout, scale, count, smooth, paint.Color, fill * weak * paint.Area.Alpha / 255d, style, false);
                    }
                    else
                    {
                        FillArea(canvas, layout, scale, count, smooth, paint.Area, fill * weak, null, style.FillMode == ChartFillMode.Gradient);
                    }
                }

                StrokeLine(canvas, layout, scale, count, smooth, paint.Color, alpha * paint.WeakOpacity, layout.Thickness, coloring ? style : null);
                if (paint.HasEmphasis)
                {
                    StrokeLast(canvas, layout, scale, count, smooth, paint.Color, alpha, layout.Thickness, coloring ? style : null);
                }
            }
        }
        finally
        {
            canvas.Restore();
        }

        if (!layout.IsBar)
        {
            DrawMarkers(canvas, layout, display, values, count, paint.Color, alpha * overlay, coloring ? style : null, live, livePhase);
        }
    }

    private static SKPoint Inside(ChartRect bounds, float x, float y, float outer) =>
        new(
            bounds.Width > outer * 2 ? Math.Clamp(x, (float)bounds.X + outer, (float)bounds.Right - outer) : x,
            bounds.Height > outer * 2 ? Math.Clamp(y, (float)bounds.Y + outer, (float)bounds.Bottom - outer) : y);

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

    private void StrokeLine(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, int count, bool smooth, SKColor color, double alpha, float thickness, ChartStyle? coloring)
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
        var paint = paints.Stroke(ChartColor.Fade(color, alpha), thickness, SKStrokeCap.Round, spacing < thickness ? SKStrokeJoin.Bevel : SKStrokeJoin.Round);
        if (coloring is null)
        {
            canvas.DrawPath(path, paint);
        }
        else
        {
            DrawRegions(canvas, layout, scale, path, paint, color, alpha, coloring, thickness);
        }

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void StrokeLast(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, int count, bool smooth, SKColor color, double alpha, float thickness, ChartStyle? coloring)
    {
        if (thickness <= 0)
        {
            return;
        }

        var end = count - 1;
        while ((end >= 0) && !IsPoint(end))
        {
            end--;
        }

        if ((end < 1) || !IsPoint(end - 1))
        {
            return;
        }

        var stroke = paints.Stroke(ChartColor.Fade(color, alpha), thickness);
        if (!smooth && (coloring is null))
        {
            canvas.DrawLine(along[end - 1], across[end - 1], along[end], across[end], stroke);
            return;
        }

        var builder = paints.Path;
        LinePathBuilder.AddLine(builder, along.AsSpan(end - 1, 2), across.AsSpan(end - 1, 2), tangents.AsSpan(end - 1, 2), smooth ? ChartLineInterpolation.Smooth : ChartLineInterpolation.Linear, false);
        using var path = builder.Detach();
        if (coloring is null)
        {
            canvas.DrawPath(path, stroke);
        }
        else
        {
            DrawRegions(canvas, layout, scale, path, stroke, color, alpha, coloring, thickness);
        }

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void FillArea(SKCanvas canvas, SparklineLayout layout, SparklineScale scale, int count, bool smooth, SKColor color, double alpha, ChartStyle? coloring, bool gradient)
    {
        if ((color.Alpha == 0) || (alpha <= 0))
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
        var fill = ChartColor.Fade(color, alpha);
        var paint = paints.Fill(fill);
        if (coloring is null)
        {
            if (gradient && TryGetTop(count, out var top))
            {
                paint.Color = SKColors.Black;
                paint.Shader = areaGradient.Update(fill, top, bottom, false);
            }

            canvas.DrawPath(path, paint);
        }
        else
        {
            DrawRegions(canvas, layout, scale, path, paint, color, alpha, coloring, 0);
        }

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);

        bool TryGetTop(int length, out float value)
        {
            value = Single.PositiveInfinity;
            for (var i = 0; i < length; i++)
            {
                if (Single.IsFinite(across[i]) && (across[i] < value))
                {
                    value = across[i];
                }
            }

            return Single.IsFinite(value);
        }
    }

    private void DrawBars(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, SparklineScale scale, SparklinePaint paint, ChartStyle style, double alpha, ChartEntranceAnimation entrance, bool coloring)
    {
        var morphing = display.IsMorphing;
        var geometry = display.Display;
        var count = morphing ? geometry.Count : values.Count;
        var slot = layout.GetSlot(scale);
        var gap = Math.Min(style.SegmentGap, slot * BarGapRatio);
        var width = (float)Math.Max(0, Math.Min(slot - gap, MaxBarWidth));
        if (width <= 0)
        {
            return;
        }

        var plot = layout.Plot;
        var winLoss = layout.Kind == ChartSparklineKind.WinLoss;
        var origin = SparklineLayout.GetBase(scale);
        var middle = (float)layout.ToY(origin, scale);
        var emphasized = paint.HasEmphasis && (layout.LastIndex >= 0) ? display.ToDisplayIndex(layout.LastIndex) : -1;
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

            var weight = !paint.HasEmphasis || (i == emphasized) ? 1 : paint.WeakOpacity;
            var color = coloring ? GetThresholdColor(layout, style, value, paint.Color) : value < 0 ? paint.Negative : paint.Color;
            canvas.DrawRect(new SKRect(center - (width / 2), top, center + (width / 2), bottom), paints.Fill(ChartColor.Fade(color, alpha * weight)));
        }
    }

    private void DrawMarkers(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, int count, SKColor color, double alpha, ChartStyle? coloring, bool live, double livePhase)
    {
        if ((color.Alpha == 0) || (alpha <= 0))
        {
            return;
        }

        var markers = layout.Markers;
        if (live && TryGetMarker(layout, display, values, count, layout.LastIndex, color, coloring, out var point, out var fill))
        {
            LiveIndicator.DrawRings(canvas, paints, point, layout.MarkerRadius, ChartColor.Fade(fill, alpha), livePhase);
            if ((markers & ChartSparklineMarkers.Last) == 0)
            {
                FillMarker(canvas, layout, point, ChartColor.Fade(fill, alpha));
            }
        }

        if (markers == ChartSparklineMarkers.None)
        {
            return;
        }

        var first = (markers & ChartSparklineMarkers.First) != 0 ? layout.FirstIndex : -1;
        var last = (markers & ChartSparklineMarkers.Last) != 0 ? layout.LastIndex : -1;
        var high = (markers & ChartSparklineMarkers.High) != 0 ? layout.HighIndex : -1;
        var low = (markers & ChartSparklineMarkers.Low) != 0 ? layout.LowIndex : -1;
        DrawMarker(canvas, layout, display, values, count, first, color, alpha, coloring);
        if (last != first)
        {
            DrawMarker(canvas, layout, display, values, count, last, color, alpha, coloring);
        }

        if ((high != first) && (high != last))
        {
            DrawMarker(canvas, layout, display, values, count, high, color, alpha, coloring);
        }

        if ((low != first) && (low != last) && (low != high))
        {
            DrawMarker(canvas, layout, display, values, count, low, color, alpha, coloring);
        }
    }

    private void DrawMarker(SKCanvas canvas, SparklineLayout layout, SparklineInterpolator display, SparklineValues values, int count, int index, SKColor color, double alpha, ChartStyle? coloring)
    {
        if (TryGetMarker(layout, display, values, count, index, color, coloring, out var point, out var fill))
        {
            FillMarker(canvas, layout, point, ChartColor.Fade(fill, alpha));
        }
    }

    private bool TryGetMarker(SparklineLayout layout, SparklineInterpolator display, SparklineValues values, int count, int index, SKColor color, ChartStyle? coloring, out SKPoint point, out SKColor fill)
    {
        var position = index >= 0 ? display.ToDisplayIndex(index) : -1;
        if ((position < 0) || (position >= count) || !Single.IsFinite(along[position]) || !Single.IsFinite(across[position]))
        {
            point = default;
            fill = default;
            return false;
        }

        point = Inside(layout.Bounds, along[position], across[position], layout.MarkerRadius + layout.MarkerKnockout);
        var value = display.IsMorphing ? display.Display.End[position] : values[index];
        fill = coloring is null ? color : GetThresholdColor(layout, coloring, value, color);
        return true;
    }

    private void FillMarker(SKCanvas canvas, SparklineLayout layout, SKPoint point, SKColor color)
    {
        if (layout.MarkerKnockout > 0)
        {
            canvas.DrawCircle(point, layout.MarkerRadius + layout.MarkerKnockout, paints.Clear);
        }

        canvas.DrawCircle(point, layout.MarkerRadius, paints.Fill(color));
    }
}
