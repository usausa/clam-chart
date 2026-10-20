namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class CartesianRenderer : IDisposable
{
    private const float TickLength = 4;

    private const float LegendLineThickness = 2;

    private const double EdgeTolerance = 0.5;

    private const float DataLabelOffset = 4;

    private const float DataLabelPadding = 1;

    private const float DataLabelSpacing = 2;

    private const float TooltipPadding = 8;

    private const float TooltipRowGap = 2;

    private const float TooltipMarkerGap = 6;

    private const float TooltipClearance = 48;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly SKRoundRect roundRect = new();
    private readonly Dictionary<LineSeries, DashEntry> dashes = [];
    private readonly Dictionary<AreaSeries, GradientEntry> gradients = [];
    private readonly List<LineSeries> staleDashes = [];
    private readonly List<AreaSeries> staleGradients = [];
    private readonly List<SKRect> placedLabels = [];
    private float[] along = [];
    private float[] across = [];
    private float[] baseAcross = [];
    private float[] tangents = [];
    private float[] baseTangents = [];
    private int frame;

    public IChartTextMeasurer Measurer => text;

    public CartesianRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
        roundRect.Dispose();
        foreach (var entry in dashes.Values)
        {
            entry.Effect?.Dispose();
        }

        foreach (var entry in gradients.Values)
        {
            entry.Shader?.Dispose();
        }

        dashes.Clear();
        gradients.Clear();
        Return(ref along);
        Return(ref across);
        Return(ref baseAcross);
        Return(ref tangents);
        Return(ref baseTangents);
    }

    public void Render(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, float width, float height)
    {
        frame++;
        var scale = canvas.TotalMatrix.ScaleX;
        var previous = GetSelection(layout, series, interaction.PreviousSeries, interaction.PreviousIndex, interaction.PreviousCategory);
        var current = GetSelection(layout, series, interaction.SelectedSeries, interaction.SelectedIndex, interaction.CategorySelection);
        var progress = interaction.SelectionProgress;
        var selection = new SelectionBlend(progress < 1 ? previous : default, current, progress < 1 ? display.Ease(progress) : 1, style.DimmedOpacity);
        DrawGrid(canvas, layout, style, scale);
        DrawBaseline(canvas, layout, style, scale);
        DrawSecondaryTicks(canvas, layout, style, scale);
        DrawSeries(canvas, layout, display, series, colors, style, selection);
        DrawMarkers(canvas, layout, display, series, colors, style, selection, width, height);
        DrawTrackball(canvas, layout, display, series, colors, style, interaction, scale, width, height);
        DrawDataLabels(canvas, layout, display, series, colors, style, selection);
        DrawAxisLabels(canvas, layout.XAxis, style);
        DrawAxisLabels(canvas, layout.YAxis, style);
        DrawTitle(canvas, layout, layout.XAxis, series, colors, style);
        DrawTitle(canvas, layout, layout.YAxis, series, colors, style);
        if (layout.HasSecondaryAxis)
        {
            DrawAxisLabels(canvas, layout.SecondaryYAxis, style);
            DrawTitle(canvas, layout, layout.SecondaryYAxis, series, colors, style);
        }

        DrawLegend(canvas, layout.Legend, series, colors, style);
        DrawTooltip(canvas, colors, style, interaction, width, height);
        RemoveStaleEntries();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    private static float Snap(double value, float scale) => (MathF.Floor((float)value * scale) + 0.5f) / scale;

    private static CartesianSelection GetSelection(CartesianLayout layout, IReadOnlyList<ChartSeries> series, int seriesIndex, int index, bool category)
    {
        if ((seriesIndex < 0) || (index < 0) || (seriesIndex >= series.Count) || (seriesIndex >= layout.Geometries.Count) || (index >= layout.Geometries[seriesIndex].Count))
        {
            return default;
        }

        var x = layout.Geometries[seriesIndex].X[index];
        var visible = category || series[seriesIndex].IsVisible;
        return visible && Double.IsFinite(x) ? new CartesianSelection(true, category, seriesIndex, index, x) : default;
    }

    private static double GetBase(AxisLayout axis) => Math.Clamp(0, axis.DisplayScale.Minimum, axis.DisplayScale.Maximum);

    private static double Grow(double value, double origin, double factor) => origin + ((value - origin) * factor);

    private static double GetGrowth(SnapshotInterpolator display, ChartEntranceAnimation entrance, int index, int count) =>
        entrance == ChartEntranceAnimation.Grow ? display.Ease(ChartProgress.ForElement(display.Progress, index, count)) : 1;

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

    private static void DrawLine(SKCanvas canvas, bool vertical, float position, ChartRect plot, SKPaint paint)
    {
        if (vertical)
        {
            canvas.DrawLine((float)plot.X, position, (float)plot.Right, position, paint);
        }
        else
        {
            canvas.DrawLine(position, (float)plot.Y, position, (float)plot.Bottom, paint);
        }
    }

    private void DrawGrid(SKCanvas canvas, CartesianLayout layout, ChartStyle style, float scale)
    {
        var thickness = (float)style.GridLineThickness;
        var paint = paints.Stroke(style.GridLineColor.ToSKColor(), thickness, SKStrokeCap.Butt);
        var snap = thickness <= 0 ? scale : 0;
        DrawGridLines(canvas, layout.XAxis, layout.PlotBounds, paint, snap);
        DrawGridLines(canvas, layout.YAxis, layout.PlotBounds, paint, snap);
        if (layout.HasSecondaryAxis)
        {
            DrawGridLines(canvas, layout.SecondaryYAxis, layout.PlotBounds, paint, snap);
        }
    }

    private static void DrawGridLines(SKCanvas canvas, AxisLayout axis, ChartRect plot, SKPaint paint, float snap)
    {
        if (!axis.Options.GridLinesVisible)
        {
            return;
        }

        var boundaries = (axis.Options.Kind == ChartAxisKind.Category) && (axis.Options.LabelPlacement == ChartLabelPlacement.BetweenTicks);
        var count = boundaries ? axis.Ticks.Count + 1 : axis.Ticks.Count;
        for (var i = 0; i < count; i++)
        {
            var value = boundaries ? i - 0.5 : axis.Ticks[i];
            if (axis.IsDisplayed(value))
            {
                var pixel = axis.ToDisplayPixel(value);
                DrawLine(canvas, axis.IsVertical, snap > 0 ? Snap(pixel, snap) : (float)pixel, plot, paint);
            }
        }
    }

    private void DrawBaseline(SKCanvas canvas, CartesianLayout layout, ChartStyle style, float scale)
    {
        if (!layout.XAxis.Options.IsVisible)
        {
            return;
        }

        var axis = layout.YAxis;
        DrawLine(canvas, axis.IsVertical, Snap(axis.ToDisplayPixel(GetBase(axis)), scale), layout.PlotBounds, paints.Stroke(style.AxisLineColor.ToSKColor(), 0, SKStrokeCap.Butt));
    }

    private void DrawSecondaryTicks(SKCanvas canvas, CartesianLayout layout, ChartStyle style, float scale)
    {
        var axis = layout.SecondaryYAxis;
        if (!layout.HasSecondaryAxis || !axis.ShowsLabels || (axis.Options.TickAlignment != ChartTickAlignment.Independent))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var paint = paints.Stroke(style.AxisLineColor.ToSKColor(), 0, SKStrokeCap.Butt);
        for (var i = 0; i < axis.Ticks.Count; i++)
        {
            if (!axis.IsDisplayed(axis.Ticks[i]))
            {
                continue;
            }

            var position = Snap(axis.ToDisplayPixel(axis.Ticks[i]), scale);
            switch (axis.Side)
            {
                case AxisSide.Right:
                    canvas.DrawLine((float)plot.Right - TickLength, position, (float)plot.Right, position, paint);
                    break;
                case AxisSide.Left:
                    canvas.DrawLine((float)plot.X, position, (float)plot.X + TickLength, position, paint);
                    break;
                case AxisSide.Top:
                    canvas.DrawLine(position, (float)plot.Y, position, (float)plot.Y + TickLength, paint);
                    break;
                default:
                    canvas.DrawLine(position, (float)plot.Bottom - TickLength, position, (float)plot.Bottom, paint);
                    break;
            }
        }
    }

    private static SKRect GetRevealClip(CartesianLayout layout, double progress, float inflate)
    {
        var plot = layout.PlotBounds;
        var axis = layout.XAxis;
        var start = (float)axis.ToDisplayPixel(axis.DisplayScale.Minimum);
        var edge = start + (((float)axis.ToDisplayPixel(axis.DisplayScale.Maximum) - start) * (float)progress);
        var low = start <= edge ? start - inflate : edge;
        var high = start <= edge ? edge : start + inflate;
        return axis.IsVertical
            ? new SKRect((float)plot.X - inflate, low, (float)plot.Right + inflate, high)
            : new SKRect(low, (float)plot.Y - inflate, high, (float)plot.Bottom + inflate);
    }

    private void DrawSeries(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, SelectionBlend selection)
    {
        var plot = layout.PlotBounds;
        var inflate = 0f;
        for (var s = 0; s < series.Count; s++)
        {
            if ((series[s] is LineSeries line) && (display.GetOpacity(s, line.IsVisible) > 0))
            {
                inflate = Math.Max(inflate, (float)(line.Thickness ?? style.LineThickness) / 2);
            }
        }

        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)plot.X - inflate, (float)plot.Y - inflate, (float)plot.Width + (inflate * 2), (float)plot.Height + (inflate * 2)));
        for (var s = 0; s < series.Count; s++)
        {
            var opacity = display.GetOpacity(s, series[s].IsVisible);
            if (opacity <= 0)
            {
                continue;
            }

            var entrance = display.GetEntrance(series[s].Kind);
            if (entrance == ChartEntranceAnimation.Fade)
            {
                opacity *= display.Ease(display.Progress);
            }

            var revealed = entrance == ChartEntranceAnimation.Reveal;
            if (revealed)
            {
                canvas.Save();
                canvas.ClipRect(GetRevealClip(layout, display.Ease(display.Progress), inflate));
            }

            if (series[s] is BarSeries bar)
            {
                DrawBars(canvas, layout, display, bar, s, colors[s], style, selection, opacity, entrance);
            }
            else if (series[s] is LineSeries line)
            {
                DrawLineSeries(canvas, layout, display, line, s, colors[s], style, opacity * selection.SeriesAlpha(s), entrance);
            }

            if (revealed)
            {
                canvas.Restore();
            }
        }

        canvas.Restore();
    }

    private void DrawLineSeries(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, LineSeries line, int seriesIndex, SKColor color, ChartStyle style, double alpha, ChartEntranceAnimation entrance)
    {
        var geometry = display.GetGeometry(layout, seriesIndex);
        var count = Project(layout, display, geometry, layout.GetValueAxis(line.YAxis), entrance);
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var interpolation = (line.Interpolation == ChartLineInterpolation.Smooth) && !geometry.IsSorted ? ChartLineInterpolation.Linear : line.Interpolation;
        if (line is AreaSeries area)
        {
            var fill = entrance == ChartEntranceAnimation.Reveal ? alpha * display.Ease(ChartProgress.Delayed(display.Progress, ChartProgress.FillDelay)) : alpha;
            FillArea(canvas, area, count, interpolation, horizontal, ChartColor.Fade(color, fill), style);
        }

        StrokeLine(canvas, line, count, interpolation, horizontal, ChartColor.Fade(color, alpha), (float)(line.Thickness ?? style.LineThickness));
    }

    private int Project(CartesianLayout layout, SnapshotInterpolator display, SeriesGeometry geometry, AxisLayout axis, ChartEntranceAnimation entrance)
    {
        var count = geometry.Count;
        Ensure(ref along, count);
        Ensure(ref across, count);
        Ensure(ref baseAcross, count);
        Ensure(ref tangents, count);
        Ensure(ref baseTangents, count);
        var plot = layout.PlotBounds;
        var vertical = layout.Orientation == ChartOrientation.Vertical;
        var low = vertical ? plot.Y : plot.X;
        var high = vertical ? plot.Bottom : plot.Right;
        var origin = GetBase(axis);
        var x = geometry.X;
        var start = geometry.Start;
        var end = geometry.End;
        for (var i = 0; i < count; i++)
        {
            var growth = GetGrowth(display, entrance, i, count);
            along[i] = Double.IsFinite(x[i]) ? (float)layout.XAxis.ToDisplayPixel(x[i]) : Single.NaN;
            across[i] = Double.IsFinite(end[i]) ? (float)axis.ToDisplayPixel(Grow(end[i], origin, growth)) : Single.NaN;
            baseAcross[i] = Double.IsFinite(start[i]) ? (float)Math.Clamp(axis.ToDisplayPixel(Grow(start[i], origin, growth)), low, high) : Single.NaN;
        }

        return count;
    }

    private void StrokeLine(SKCanvas canvas, LineSeries line, int count, ChartLineInterpolation interpolation, bool horizontal, SKColor color, float thickness)
    {
        if (thickness <= 0)
        {
            return;
        }

        var builder = paints.Path;
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
            if (interpolation == ChartLineInterpolation.Smooth)
            {
                LinePathBuilder.ComputeTangents(runAlong, runAcross, runTangents);
            }

            LinePathBuilder.AddLine(builder, runAlong, runAcross, runTangents, interpolation, horizontal);
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
        var paint = paints.Stroke(color, thickness, SKStrokeCap.Round, spacing < thickness ? SKStrokeJoin.Bevel : SKStrokeJoin.Round);
        paint.PathEffect = GetDash(line, thickness);
        canvas.DrawPath(path, paint);

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void FillArea(SKCanvas canvas, AreaSeries area, int count, ChartLineInterpolation interpolation, bool horizontal, SKColor color, ChartStyle style)
    {
        var opacity = area.FillOpacity ?? style.AreaFillOpacity;
        if (opacity <= 0)
        {
            return;
        }

        var builder = paints.Path;
        var drawn = false;
        var baseline = Single.NaN;
        var extreme = Single.NaN;
        for (var start = 0; start < count;)
        {
            if (!IsFilled(start))
            {
                start++;
                continue;
            }

            var end = start;
            while ((end < count) && IsFilled(end))
            {
                end++;
            }

            if (end - start >= 2)
            {
                var length = end - start;
                var runAlong = along.AsSpan(start, length);
                var runAcross = across.AsSpan(start, length);
                var runBase = baseAcross.AsSpan(start, length);
                var runTangents = tangents.AsSpan(start, length);
                var runBaseTangents = baseTangents.AsSpan(start, length);
                if (interpolation == ChartLineInterpolation.Smooth)
                {
                    LinePathBuilder.ComputeTangents(runAlong, runAcross, runTangents);
                    LinePathBuilder.ComputeTangents(runAlong, runBase, runBaseTangents);
                }

                LinePathBuilder.AddLine(builder, runAlong, runAcross, runTangents, interpolation, horizontal);
                LinePathBuilder.AddReversedLine(builder, runAlong, runBase, runBaseTangents, interpolation, horizontal);
                builder.Close();
                drawn = true;
                for (var i = 0; i < length; i++)
                {
                    baseline = Single.IsNaN(baseline) ? runBase[i] : baseline;
                    if (Single.IsNaN(extreme) || (Math.Abs(runAcross[i] - baseline) > Math.Abs(extreme - baseline)))
                    {
                        extreme = runAcross[i];
                    }
                }
            }

            start = end;
        }

        if (!drawn)
        {
            return;
        }

        using var path = builder.Detach();
        var fill = color.WithAlpha((byte)Math.Round(color.Alpha * opacity));
        var paint = paints.Fill(fill);
        if (area.FillMode == ChartFillMode.Gradient)
        {
            paint.Shader = GetGradient(area, fill, extreme, baseline, horizontal);
        }

        canvas.DrawPath(path, paint);

        bool IsFilled(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]) && Single.IsFinite(baseAcross[index]);
    }

    private void DrawBars(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, BarSeries bar, int seriesIndex, SKColor color, ChartStyle style, SelectionBlend selection, double opacity, ChartEntranceAnimation entrance)
    {
        var geometry = display.GetGeometry(layout, seriesIndex);
        if (geometry.BarSlot < 0)
        {
            return;
        }

        var target = layout.Geometries[seriesIndex];
        var axis = layout.GetValueAxis(bar.YAxis);
        var negative = bar.NegativeColor is { } negativeColor ? negativeColor.ToSKColor() : color;
        var radius = (float)(bar.CornerRadius ?? style.BarCornerRadius);
        var gap = (float)style.SegmentGap;
        var values = geometry.Value;
        for (var i = 0; i < geometry.Count; i++)
        {
            if (!TryGetBar(layout, geometry, axis, i, gap, GetGrowth(display, entrance, i, geometry.Count), out var shape))
            {
                continue;
            }

            var index = display.ToTargetIndex(seriesIndex, i);
            var alpha = opacity * selection.PointAlpha(seriesIndex, index, index >= 0 ? target.X[index] : Double.NaN);
            DrawBar(canvas, shape, radius, paints.Fill(ChartColor.Fade(values[i] < 0 ? negative : color, alpha)));
        }
    }

    private static bool TryGetBar(CartesianLayout layout, SeriesGeometry geometry, AxisLayout axis, int index, float gap, double growth, out BarShape shape)
    {
        shape = default;
        var x = geometry.X[index];
        var start = geometry.Start[index];
        var end = geometry.End[index];
        if (!Double.IsFinite(x) || !Double.IsFinite(start) || !Double.IsFinite(end))
        {
            return false;
        }

        var plot = layout.PlotBounds;
        var vertical = layout.Orientation == ChartOrientation.Vertical;
        var low = (float)(vertical ? plot.Y : plot.X);
        var high = (float)(vertical ? plot.Bottom : plot.Right);
        var origin = GetBase(axis);
        var (from, to) = geometry.GetBarRange(x);
        var along1 = (float)layout.XAxis.ToDisplayPixel(from);
        var along2 = (float)layout.XAxis.ToDisplayPixel(to);
        var basePixel = Math.Clamp((float)axis.ToDisplayPixel(Grow(start, origin, growth)), low, high);
        var dataPixel = (float)axis.ToDisplayPixel(Grow(end, origin, growth));
        var endPixel = Math.Clamp(dataPixel, low, high);
        var direction = MathF.Sign(endPixel - basePixel);
        if (direction == 0)
        {
            return false;
        }

        var outer = !geometry.IsStacked || geometry.Outer[index];
        if (geometry.IsStacked)
        {
            if (!start.Equals(0d))
            {
                basePixel += direction * gap / 2;
            }

            if (!outer)
            {
                endPixel -= direction * gap / 2;
            }

            if (MathF.Sign(endPixel - basePixel) != direction)
            {
                return false;
            }
        }

        var acrossStart = Math.Min(basePixel, endPixel);
        var acrossEnd = Math.Max(basePixel, endPixel);
        var rect = vertical
            ? new SKRect(Math.Min(along1, along2), acrossStart, Math.Max(along1, along2), acrossEnd)
            : new SKRect(acrossStart, Math.Min(along1, along2), acrossEnd, Math.Max(along1, along2));
        var edge = vertical ? (direction < 0 ? BarEdge.Top : BarEdge.Bottom) : (direction < 0 ? BarEdge.Left : BarEdge.Right);
        shape = new BarShape(rect, edge, outer && endPixel.Equals(dataPixel));
        return true;
    }

    private void DrawBar(SKCanvas canvas, in BarShape shape, float radius, SKPaint paint)
    {
        var rect = shape.Rect;
        var vertical = shape.Edge is BarEdge.Top or BarEdge.Bottom;
        var thickness = vertical ? rect.Width : rect.Height;
        var length = vertical ? rect.Height : rect.Width;
        var corner = shape.Rounded ? Math.Min(radius, Math.Min(thickness / 2, length)) : 0;
        if (corner <= 0)
        {
            canvas.DrawRect(rect, paint);
            return;
        }

        Span<SKPoint> radii = stackalloc SKPoint[4];
        var point = new SKPoint(corner, corner);
        switch (shape.Edge)
        {
            case BarEdge.Top:
                radii[0] = point;
                radii[1] = point;
                break;
            case BarEdge.Bottom:
                radii[2] = point;
                radii[3] = point;
                break;
            case BarEdge.Left:
                radii[0] = point;
                radii[3] = point;
                break;
            default:
                radii[1] = point;
                radii[2] = point;
                break;
        }

        roundRect.SetRectRadii(rect, radii);
        canvas.DrawRoundRect(roundRect, paint);
    }

    private void DrawMarkers(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, SelectionBlend selection, float width, float height)
    {
        var overlay = display.OverlayAlpha;
        if (overlay <= 0)
        {
            return;
        }

        var plot = layout.PlotBounds;
        var knockout = (float)style.MarkerKnockout;
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        for (var s = 0; s < series.Count; s++)
        {
            var opacity = display.GetOpacity(s, series[s].IsVisible) * overlay;
            if ((series[s] is not LineSeries line) || (opacity <= 0) || (line.MarkerMode == ChartMarkerMode.None))
            {
                continue;
            }

            var radius = (float)(line.MarkerSize ?? style.MarkerSize) / 2;
            if (radius <= 0)
            {
                continue;
            }

            var color = ChartColor.Fade(colors[s], opacity * selection.SeriesAlpha(s));
            var count = Project(layout, display, display.GetGeometry(layout, s), layout.GetValueAxis(line.YAxis), display.GetEntrance(line.Kind));
            for (var i = count - 1; i >= 0; i--)
            {
                if (!Single.IsFinite(along[i]) || !Single.IsFinite(across[i]))
                {
                    continue;
                }

                DrawMarkerInPlot(canvas, plot, LinePathBuilder.ToPoint(along[i], across[i], horizontal), radius, knockout, color, width, height);
                if ((line.MarkerMode == ChartMarkerMode.Auto) || layout.LastMarkerOnly)
                {
                    break;
                }
            }

            var selected = display.ToDisplayIndex(s, GetSelectedIndex(layout, s, selection.Current));
            if ((selected >= 0) && (selected < count) && Single.IsFinite(along[selected]) && Single.IsFinite(across[selected]))
            {
                var scale = 0.6f + (0.4f * (float)selection.Progress);
                DrawMarkerInPlot(canvas, plot, LinePathBuilder.ToPoint(along[selected], across[selected], horizontal), radius * scale, knockout * scale, ChartColor.Fade(colors[s], opacity), width, height);
            }
        }
    }

    private static int GetSelectedIndex(CartesianLayout layout, int seriesIndex, CartesianSelection selection)
    {
        if (!selection.Active)
        {
            return -1;
        }

        if (!selection.Category)
        {
            return selection.Series == seriesIndex ? selection.Index : -1;
        }

        var index = CartesianHitMap.FindNearestIndexAt(layout, seriesIndex, selection.X);
        return (index >= 0) && layout.Geometries[seriesIndex].X[index].Equals(selection.X) ? index : -1;
    }

    private void DrawMarkerInPlot(SKCanvas canvas, ChartRect plot, SKPoint point, float radius, float knockout, SKColor color, float width, float height)
    {
        if ((point.X >= plot.X - EdgeTolerance) && (point.X <= plot.Right + EdgeTolerance) && (point.Y >= plot.Y - EdgeTolerance) && (point.Y <= plot.Bottom + EdgeTolerance))
        {
            DrawMarker(canvas, point, radius, knockout, color, width, height);
        }
    }

    private void DrawTrackball(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, float scale, float width, float height)
    {
        var lead = interaction.LeadSeries;
        if (!interaction.IsTracking || (lead < 0) || (lead >= layout.Geometries.Count))
        {
            return;
        }

        var leadGeometry = display.GetGeometry(layout, lead);
        var leadIndex = display.ToDisplayIndex(lead, interaction.LeadIndex);
        if ((leadIndex < 0) || (leadIndex >= leadGeometry.Count))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var position = Snap(layout.XAxis.ToDisplayPixel(leadGeometry.X[leadIndex]), scale);
        DrawLine(canvas, layout.XAxis.IsVertical, position, plot, paints.Stroke(style.TrackballLineColor.ToSKColor(), 0, SKStrokeCap.Butt));
        var indices = interaction.TrackedIndices;
        var knockout = (float)style.MarkerKnockout;
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        for (var s = 0; (s < indices.Count) && (s < series.Count); s++)
        {
            var geometry = display.GetGeometry(layout, s);
            var index = display.ToDisplayIndex(s, indices[s]);
            if ((index < 0) || (series[s] is not LineSeries line) || !line.IsVisible || (index >= geometry.Count))
            {
                continue;
            }

            var radius = (float)(line.MarkerSize ?? style.MarkerSize) / 2;
            if (radius > 0)
            {
                var point = LinePathBuilder.ToPoint((float)layout.XAxis.ToDisplayPixel(geometry.X[index]), (float)layout.GetValueAxis(line.YAxis).ToDisplayPixel(geometry.End[index]), horizontal);
                DrawMarkerInPlot(canvas, plot, point, radius, knockout, colors[s], width, height);
            }
        }
    }

    private void DrawTooltip(SKCanvas canvas, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, float width, float height)
    {
        if (!interaction.IsTracking || !interaction.TooltipVisible || (interaction.TooltipHeader is not { } header))
        {
            return;
        }

        var size = style.TooltipFontSize;
        var lineHeight = (float)text.GetLineHeight(size);
        var marker = (float)style.LegendMarkerSize;
        var texts = interaction.TrackedTexts;
        var rows = 1;
        var content = (float)text.Measure(header, size, Double.PositiveInfinity);
        for (var s = 0; (s < texts.Count) && (s < colors.Count); s++)
        {
            if (texts[s] is { } row)
            {
                rows++;
                content = Math.Max(content, marker + TooltipMarkerGap + (float)text.Measure(row, size, Double.PositiveInfinity));
            }
        }

        var boxWidth = content + (TooltipPadding * 2);
        var boxHeight = (rows * lineHeight) + ((rows - 1) * TooltipRowGap) + (TooltipPadding * 2);
        var finger = interaction.TrackPoint;
        var left = boxWidth < width ? Math.Clamp((float)finger.X - (boxWidth / 2), 0, width - boxWidth) : 0;
        var top = (float)finger.Y - TooltipClearance - boxHeight;
        if (top < 0)
        {
            top = (float)finger.Y + TooltipClearance;
        }

        if (top + boxHeight > height)
        {
            top = Math.Max(0, height - boxHeight);
        }

        var box = SKRect.Create(left, top, boxWidth, boxHeight);
        var corner = (float)style.TooltipCornerRadius;
        canvas.DrawRoundRect(box, corner, corner, paints.Fill(style.TooltipBackground.ToSKColor()));
        canvas.DrawRoundRect(box, corner, corner, paints.Stroke(style.TooltipBorderColor.ToSKColor(), 0, SKStrokeCap.Butt));
        var color = style.TooltipTextColor.ToSKColor();
        var x = left + TooltipPadding;
        var y = top + TooltipPadding;
        DrawText(canvas, header, new ChartRect(x, y, content, lineHeight), size, Double.PositiveInfinity, color, style, false);
        for (var s = 0; (s < texts.Count) && (s < colors.Count); s++)
        {
            if (texts[s] is not { } row)
            {
                continue;
            }

            y += lineHeight + TooltipRowGap;
            canvas.DrawCircle(x + (marker / 2), y + (lineHeight / 2), marker / 2, paints.Fill(colors[s]));
            DrawText(canvas, row, new ChartRect(x + marker + TooltipMarkerGap, y, content - marker - TooltipMarkerGap, lineHeight), size, Double.PositiveInfinity, color, style, false);
        }
    }

    private void DrawMarker(SKCanvas canvas, SKPoint point, float radius, float knockout, SKColor color, float width, float height)
    {
        var outer = radius + knockout;
        var x = width > outer * 2 ? Math.Clamp(point.X, outer, width - outer) : point.X;
        var y = height > outer * 2 ? Math.Clamp(point.Y, outer, height - outer) : point.Y;
        if (knockout > 0)
        {
            canvas.DrawCircle(x, y, outer, paints.Clear);
        }

        canvas.DrawCircle(x, y, radius, paints.Fill(color));
    }

    private void DrawDataLabels(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, SelectionBlend selection)
    {
        placedLabels.Clear();
        var overlay = display.OverlayAlpha;
        if (overlay <= 0)
        {
            return;
        }

        var labels = layout.DataLabels;
        var height = (float)layout.DataLabelHeight;
        var outside = style.SecondaryTextColor.ToSKColor();
        var gap = (float)style.SegmentGap;
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            var item = series[label.SeriesIndex];
            var alpha = overlay * display.GetOpacity(label.SeriesIndex, item.IsVisible);
            var geometry = display.GetGeometry(layout, label.SeriesIndex);
            var index = display.ToDisplayIndex(label.SeriesIndex, label.Index);
            if ((alpha <= 0) || (index < 0) || (index >= geometry.Count))
            {
                continue;
            }

            var axis = layout.GetValueAxis(item.YAxis);
            var growth = GetGrowth(display, display.GetEntrance(item.Kind), index, geometry.Count);
            var size = new SKSize((float)label.Width, height);
            SKRect bounds;
            SKColor color;
            if (item is BarSeries bar)
            {
                if (!TryGetBar(layout, geometry, axis, index, gap, growth, out var shape) || !TryPlaceBarLabel(layout.PlotBounds, geometry.IsStacked, shape, size, out bounds, out var inside))
                {
                    continue;
                }

                var fill = (geometry.Value[index] < 0) && (bar.NegativeColor is { } negative) ? negative.ToSKColor() : colors[label.SeriesIndex];
                color = inside ? ChartColor.GetContrastColor(fill) : outside;
                if (inside)
                {
                    alpha *= selection.PointAlpha(label.SeriesIndex, label.Index, layout.Geometries[label.SeriesIndex].X[label.Index]);
                }
            }
            else if ((item is LineSeries line) && TryPlaceLineLabel(layout, geometry, axis, line, index, growth, size, style, out bounds))
            {
                color = outside;
            }
            else
            {
                continue;
            }

            placedLabels.Add(bounds);
            DrawText(canvas, label.Text, new ChartRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height), style.DataLabelFontSize, Double.PositiveInfinity, ChartColor.Fade(color, alpha), style);
        }
    }

    private bool TryPlaceBarLabel(ChartRect plot, bool stacked, in BarShape shape, SKSize size, out SKRect bounds, out bool inside)
    {
        var bar = shape.Rect;
        inside = true;
        if (stacked)
        {
            bounds = SKRect.Create(bar.MidX - (size.Width / 2), bar.MidY - (size.Height / 2), size.Width, size.Height);
            return FitsInside(bar, bounds) && IsFree(bounds);
        }

        var left = Shift(bar.MidX - (size.Width / 2), size.Width, plot.X, plot.Right);
        var top = Shift(bar.MidY - (size.Height / 2), size.Height, plot.Y, plot.Bottom);
        bounds = shape.Edge switch
        {
            BarEdge.Top => SKRect.Create(left, bar.Top - DataLabelOffset - size.Height, size.Width, size.Height),
            BarEdge.Bottom => SKRect.Create(left, bar.Bottom + DataLabelOffset, size.Width, size.Height),
            BarEdge.Left => SKRect.Create(bar.Left - DataLabelOffset - size.Width, top, size.Width, size.Height),
            _ => SKRect.Create(bar.Right + DataLabelOffset, top, size.Width, size.Height)
        };
        if (IsInside(plot, bounds) && IsFree(bounds))
        {
            inside = false;
            return true;
        }

        bounds = shape.Edge switch
        {
            BarEdge.Top => SKRect.Create(bar.MidX - (size.Width / 2), bar.Top + DataLabelOffset, size.Width, size.Height),
            BarEdge.Bottom => SKRect.Create(bar.MidX - (size.Width / 2), bar.Bottom - DataLabelOffset - size.Height, size.Width, size.Height),
            BarEdge.Left => SKRect.Create(bar.Left + DataLabelOffset, bar.MidY - (size.Height / 2), size.Width, size.Height),
            _ => SKRect.Create(bar.Right - DataLabelOffset - size.Width, bar.MidY - (size.Height / 2), size.Width, size.Height)
        };
        return FitsInside(bar, bounds) && IsFree(bounds);
    }

    private bool TryPlaceLineLabel(CartesianLayout layout, SeriesGeometry geometry, AxisLayout axis, LineSeries line, int index, double growth, SKSize size, ChartStyle style, out SKRect bounds)
    {
        bounds = default;
        var plot = layout.PlotBounds;
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var point = LinePathBuilder.ToPoint((float)layout.XAxis.ToDisplayPixel(geometry.X[index]), (float)axis.ToDisplayPixel(Grow(geometry.End[index], GetBase(axis), growth)), horizontal);
        if (!IsInside(plot, new SKRect(point.X, point.Y, point.X, point.Y)))
        {
            return false;
        }

        var radius = line.MarkerMode == ChartMarkerMode.None ? 0 : (float)(line.MarkerSize ?? style.MarkerSize) / 2;
        var marker = radius > 0 ? radius + (float)style.MarkerKnockout : 0;
        var offset = Math.Max(marker, (float)(line.Thickness ?? style.LineThickness) / 2) + DataLabelOffset;
        if (horizontal)
        {
            var top = Shift(point.Y - (size.Height / 2), size.Height, plot.Y, plot.Bottom);
            bounds = SKRect.Create(point.X + offset, top, size.Width, size.Height);
            if (IsInside(plot, bounds) && IsFree(bounds))
            {
                return true;
            }

            bounds = SKRect.Create(point.X - offset - size.Width, top, size.Width, size.Height);
        }
        else
        {
            var left = Shift(point.X - (size.Width / 2), size.Width, plot.X, plot.Right);
            bounds = SKRect.Create(left, point.Y - offset - size.Height, size.Width, size.Height);
            if (IsInside(plot, bounds) && IsFree(bounds))
            {
                return true;
            }

            bounds = SKRect.Create(left, point.Y + offset, size.Width, size.Height);
        }

        return IsInside(plot, bounds) && IsFree(bounds);
    }

    private static float Shift(float start, float length, double low, double high) =>
        length <= high - low ? (float)Math.Clamp(start, low, high - length) : start;

    private static bool IsInside(ChartRect plot, SKRect bounds) =>
        (bounds.Left >= plot.X - EdgeTolerance) && (bounds.Right <= plot.Right + EdgeTolerance) && (bounds.Top >= plot.Y - EdgeTolerance) && (bounds.Bottom <= plot.Bottom + EdgeTolerance);

    private static bool FitsInside(SKRect bar, SKRect bounds) =>
        (bounds.Left >= bar.Left + DataLabelPadding) && (bounds.Right <= bar.Right - DataLabelPadding) && (bounds.Top >= bar.Top + DataLabelPadding) && (bounds.Bottom <= bar.Bottom - DataLabelPadding);

    private bool IsFree(SKRect bounds)
    {
        foreach (var placed in placedLabels)
        {
            var area = placed;
            area.Inflate(DataLabelSpacing, DataLabelSpacing);
            if (area.IntersectsWith(bounds))
            {
                return false;
            }
        }

        return true;
    }

    private void DrawAxisLabels(SKCanvas canvas, AxisLayout axis, ChartStyle style)
    {
        var color = style.MutedTextColor.ToSKColor();
        for (var i = 0; i < axis.Labels.Count; i++)
        {
            var label = axis.Labels[i];
            if (axis.IsDisplayed(label.Value))
            {
                DrawText(canvas, label.Text, axis.GetDisplayBounds(label), style.AxisLabelFontSize, label.MaxWidth, color, style);
            }
        }
    }

    private void DrawTitle(SKCanvas canvas, CartesianLayout layout, AxisLayout axis, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style)
    {
        if (!axis.ShowsTitle || (axis.Options.Title is not { } title))
        {
            return;
        }

        DrawText(canvas, title, axis.TitleBounds, style.AxisTitleFontSize, axis.TitleMaxWidth, style.SecondaryTextColor.ToSKColor(), style);
        var markers = axis.TitleMarkers;
        var index = 0;
        for (var s = 0; (s < series.Count) && (index < markers.Count); s++)
        {
            if (series[s].IsVisible && (layout.GetValueAxis(series[s].YAxis) == axis))
            {
                var marker = markers[index++];
                canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Width / 2, paints.Fill(colors[s]));
            }
        }
    }

    private void DrawLegend(SKCanvas canvas, LegendLayout legend, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style)
    {
        if (!legend.IsVisible)
        {
            return;
        }

        for (var i = 0; i < legend.Items.Count; i++)
        {
            var item = legend.Items[i];
            var source = series[item.SeriesIndex];
            var color = source.IsVisible ? colors[item.SeriesIndex] : colors[item.SeriesIndex].WithAlpha((byte)Math.Round(colors[item.SeriesIndex].Alpha * style.DimmedOpacity));
            var marker = item.Marker;
            if (source.Kind == ChartSeriesKind.Line)
            {
                canvas.DrawLine((float)marker.X, (float)marker.CenterY, (float)marker.Right, (float)marker.CenterY, paints.Stroke(color, LegendLineThickness));
            }
            else
            {
                canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Height / 2, paints.Fill(color));
            }

            var textColor = source.IsVisible ? style.SecondaryTextColor : style.MutedTextColor;
            DrawText(canvas, source.Title ?? String.Empty, item.Text, style.LegendFontSize, item.TextMaxWidth, textColor.ToSKColor(), style);
        }

        if (legend.OverflowCount > 0)
        {
            DrawText(canvas, legend.OverflowText, legend.OverflowBounds, style.LegendFontSize, Double.PositiveInfinity, style.MutedTextColor.ToSKColor(), style);
        }
    }

    private void DrawText(SKCanvas canvas, string value, ChartRect bounds, double fontSize, double maxWidth, SKColor color, ChartStyle style, bool halo = true) =>
        text.DrawText(canvas, value, bounds, fontSize, maxWidth, color, style, halo);

    private SKPathEffect? GetDash(LineSeries line, float thickness)
    {
        var pattern = line.DashPattern;
        if ((pattern is null) || (pattern.Count == 0))
        {
            return null;
        }

        if (!dashes.TryGetValue(line, out var entry))
        {
            entry = new DashEntry();
            dashes.Add(line, entry);
        }

        entry.Frame = frame;
        var length = pattern.Count % 2 == 0 ? pattern.Count : pattern.Count * 2;
        var changed = entry.Intervals.Length != length;
        if (changed)
        {
            entry.Intervals = new float[length];
        }

        var total = 0f;
        for (var i = 0; i < length; i++)
        {
            var value = (float)Math.Max(0, pattern[i % pattern.Count]) * thickness;
            changed |= !entry.Intervals[i].Equals(value);
            entry.Intervals[i] = value;
            total += value;
        }

        if (!(total > 0))
        {
            return null;
        }

        if (changed || (entry.Effect is null))
        {
            entry.Effect?.Dispose();
            entry.Effect = SKPathEffect.CreateDash(entry.Intervals, 0);
        }

        return entry.Effect;
    }

    private SKShader GetGradient(AreaSeries area, SKColor color, float from, float to, bool horizontal)
    {
        if (!gradients.TryGetValue(area, out var entry))
        {
            entry = new GradientEntry();
            gradients.Add(area, entry);
        }

        entry.Frame = frame;
        if ((entry.Shader is null) || (entry.Color != color) || !entry.From.Equals(from) || !entry.To.Equals(to) || (entry.Horizontal != horizontal))
        {
            entry.Shader?.Dispose();
            entry.Color = color;
            entry.From = from;
            entry.To = to;
            entry.Horizontal = horizontal;
            entry.Shader = SKShader.CreateLinearGradient(
                LinePathBuilder.ToPoint(0, from, horizontal),
                LinePathBuilder.ToPoint(0, to, horizontal),
                [color, color.WithAlpha(0)],
                SKShaderTileMode.Clamp);
        }

        return entry.Shader;
    }

    private void RemoveStaleEntries()
    {
        staleDashes.Clear();
        foreach (var (line, entry) in dashes)
        {
            if (entry.Frame != frame)
            {
                entry.Effect?.Dispose();
                staleDashes.Add(line);
            }
        }

        foreach (var line in staleDashes)
        {
            dashes.Remove(line);
        }

        staleGradients.Clear();
        foreach (var (area, entry) in gradients)
        {
            if (entry.Frame != frame)
            {
                entry.Shader?.Dispose();
                staleGradients.Add(area);
            }
        }

        foreach (var area in staleGradients)
        {
            gradients.Remove(area);
        }
    }

    private sealed class DashEntry
    {
        public float[] Intervals { get; set; } = [];

        public SKPathEffect? Effect { get; set; }

        public int Frame { get; set; }
    }

    private sealed class GradientEntry
    {
        public SKShader? Shader { get; set; }

        public SKColor Color { get; set; }

        public float From { get; set; }

        public float To { get; set; }

        public bool Horizontal { get; set; }

        public int Frame { get; set; }
    }
}
