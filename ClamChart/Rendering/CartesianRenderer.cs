namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed partial class CartesianRenderer : IDisposable
{
    private const float TickLength = 4;

    private const float LegendLineThickness = 2;

    private const float SortArrowThickness = 1.25f;

    private const double EdgeTolerance = 0.5;

    private const float DataLabelOffset = 4;

    private const float DataLabelPadding = 1;

    private const float DataLabelSpacing = 2;

    private const int MaxGlowPoints = 500;

    private const byte BarGradientTail = 102;

    private readonly PaintCache paints = new();
    private readonly SKPaint glowPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
    private readonly ChartTextPainter text;
    private readonly SKRoundRect roundRect = new();
    private readonly Dictionary<LineSeries, DashEntry> dashes = [];
    private readonly Dictionary<AreaSeries, GradientEntry> gradients = [];
    private readonly Dictionary<LineSeries, GlowEntry> glows = [];
    private readonly Dictionary<BarSeries, BarGradientEntry> barGradients = [];
    private readonly HashSet<(int Slot, double X)> drawnTracks = [];
    private readonly List<LineSeries> staleDashes = [];
    private readonly List<AreaSeries> staleGradients = [];
    private readonly List<LineSeries> staleGlows = [];
    private readonly List<BarSeries> staleBarGradients = [];
    private readonly List<SKRect> placedLabels = [];
    private float[] along = [];
    private float[] across = [];
    private float[] baseAcross = [];
    private float[] tangents = [];
    private float[] baseTangents = [];
    private float[] orderKeys = [];
    private int[] drawOrder = [];
    private int frame;

    public IChartTextMeasurer Measurer => text;

    public CartesianRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        glowPaint.Dispose();
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

        foreach (var entry in glows.Values)
        {
            entry.Filter?.Dispose();
        }

        foreach (var entry in barGradients.Values)
        {
            entry.Dispose();
        }

        dashes.Clear();
        gradients.Clear();
        glows.Clear();
        barGradients.Clear();
        DisposeOverlays();
        Return(ref along);
        Return(ref across);
        Return(ref baseAcross);
        Return(ref tangents);
        Return(ref baseTangents);
        Return(ref orderKeys);
        Return(ref drawOrder);
    }

    public void Render(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, CartesianRenderState state, float width, float height)
    {
        frame++;
        var scale = canvas.TotalMatrix.ScaleX;
        var previous = GetSelection(layout, series, interaction.PreviousSeries, interaction.PreviousIndex, interaction.PreviousCategory);
        var current = GetSelection(layout, series, interaction.SelectedSeries, interaction.SelectedIndex, interaction.CategorySelection);
        var progress = interaction.SelectionProgress;
        var selection = new SelectionBlend(progress < 1 ? previous : default, current, progress < 1 ? display.Ease(progress) : 1, style.DimmedOpacity);
        DrawGrid(canvas, layout, scale);
        DrawBaseline(canvas, layout, scale);
        DrawSecondaryTicks(canvas, layout, scale);
        DrawBands(canvas, layout, state, style);
        DrawThresholdLines(canvas, layout, state, style);
        DrawSeries(canvas, layout, display, series, colors, style, selection, state);
        DrawReferenceLines(canvas, layout, state, style);
        DrawAnnotationLabels(canvas, layout, state, style);
        DrawMarkers(canvas, layout, display, series, colors, style, selection, state, width, height);
        if (interaction.IsCrosshair)
        {
            DrawCrosshair(canvas, layout, display, series, colors, style, interaction, state, scale, width, height);
        }
        else
        {
            DrawTrackball(canvas, layout, display, series, colors, style, interaction, state, scale, width, height);
        }

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

        DrawLegend(canvas, layout.Legend, series, colors, style, scale);
        DrawTooltip(canvas, interaction.IsTracking ? GetTooltipColors(layout, series, colors, style, interaction, state) : colors, style, interaction, width, height);
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

    private static double GetCornerRadius(ChartSeries item) => item switch
    {
        BarSeries bar => bar.CornerRadius,
        RangeBarSeries range => range.CornerRadius,
        _ => 0
    };

    private static bool IsTrackVisible(ChartSeries item) => item is BarSeries { TrackVisible: true } or RangeBarSeries { TrackVisible: true };

    private static BarEdge GetOpposite(BarEdge edge) => edge switch
    {
        BarEdge.Top => BarEdge.Bottom,
        BarEdge.Bottom => BarEdge.Top,
        BarEdge.Left => BarEdge.Right,
        _ => BarEdge.Left
    };

    private static void SetEdgeRadii(Span<SKPoint> radii, BarEdge edge, SKPoint point)
    {
        switch (edge)
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
    }

    private static void Ensure<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            Return(ref buffer);
            buffer = DataSnapshot.Rent<T>(count);
        }
    }

    private static void Return<T>(ref T[] buffer)
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

    private void DrawGrid(SKCanvas canvas, CartesianLayout layout, float scale)
    {
        DrawAxisGrid(canvas, layout.XAxis, layout.PlotBounds, scale);
        DrawAxisGrid(canvas, layout.YAxis, layout.PlotBounds, scale);
        if (layout.HasSecondaryAxis)
        {
            DrawAxisGrid(canvas, layout.SecondaryYAxis, layout.PlotBounds, scale);
        }
    }

    private void DrawAxisGrid(SKCanvas canvas, AxisLayout axis, ChartRect bounds, float scale)
    {
        var thickness = (float)axis.Options.GridLineThickness;
        var paint = paints.Stroke(axis.Options.GridLineColor.ToSKColor(), thickness, SKStrokeCap.Butt);
        DrawGridLines(canvas, axis, bounds, paint, thickness <= 0 ? scale : 0);
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

    private void DrawBaseline(SKCanvas canvas, CartesianLayout layout, float scale)
    {
        if (!layout.XAxis.Options.IsVisible)
        {
            return;
        }

        var axis = layout.YAxis;
        DrawLine(canvas, axis.IsVertical, Snap(axis.ToDisplayPixel(GetBase(axis)), scale), layout.PlotBounds, paints.Stroke(axis.Options.LineColor.ToSKColor(), 0, SKStrokeCap.Butt));
    }

    private void DrawSecondaryTicks(SKCanvas canvas, CartesianLayout layout, float scale)
    {
        var axis = layout.SecondaryYAxis;
        if (!layout.HasSecondaryAxis || !axis.ShowsLabels || (axis.Options.TickAlignment != ChartTickAlignment.Independent))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var paint = paints.Stroke(axis.Options.LineColor.ToSKColor(), 0, SKStrokeCap.Butt);
        // ReSharper disable once ForCanBeConvertedToForeach
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

    private void DrawSeries(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, SelectionBlend selection, CartesianRenderState state)
    {
        var plot = layout.PlotBounds;
        var inflate = 0f;
        for (var s = 0; s < series.Count; s++)
        {
            if (display.GetOpacity(s, series[s].IsVisible) <= 0)
            {
                continue;
            }

            if (series[s] is LineSeries line)
            {
                inflate = Math.Max(inflate, (float)line.Thickness / 2);
            }
            else if (series[s] is ScatterSeries scatter and not BubbleSeries)
            {
                inflate = Math.Max(inflate, ((float)scatter.MarkerSize / 2) + ScatterStrokeWidth);
            }
        }

        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)plot.X - inflate, (float)plot.Y - inflate, (float)plot.Width + (inflate * 2), (float)plot.Height + (inflate * 2)));
        DrawBarTracks(canvas, layout, display, series, style);
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

            if (series[s].Kind is ChartSeriesKind.Bar or ChartSeriesKind.RangeBar)
            {
                DrawBars(canvas, layout, display, series[s], s, colors[s], style, selection, opacity, entrance, state);
            }
            else if (series[s] is LineSeries line)
            {
                DrawLineSeries(canvas, layout, display, line, s, colors[s], style, opacity * selection.SeriesAlpha(s), entrance, state, inflate);
            }
            else if (series[s] is ScatterSeries scatter)
            {
                DrawScatter(canvas, layout, display, scatter, s, colors[s], style, selection, opacity, entrance, state);
            }

            if (revealed)
            {
                canvas.Restore();
            }
        }

        canvas.Restore();
    }

    private void DrawLineSeries(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, LineSeries line, int seriesIndex, SKColor color, ChartStyle style, double alpha, ChartEntranceAnimation entrance, CartesianRenderState state, float inflate)
    {
        var geometry = display.GetGeometry(layout, seriesIndex);
        var count = Project(layout, display, geometry, layout.GetValueAxis(line.YAxis), entrance);
        if ((state.Downsampling == ChartDownsampling.Auto) && geometry.IsSorted)
        {
            count = LttbDownsampler.Reduce(along, across, baseAcross, count);
        }

        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var interpolation = (line.Interpolation == ChartLineInterpolation.Smooth) && !geometry.IsSorted ? ChartLineInterpolation.Linear : line.Interpolation;
        var coloring = IsColoring(layout, state, line.YAxis) ? layout : null;
        if (line is AreaSeries area)
        {
            var fill = entrance == ChartEntranceAnimation.Reveal ? alpha * display.Ease(ChartProgress.Delayed(display.Progress, ChartProgress.FillDelay)) : alpha;
            FillArea(canvas, area, count, interpolation, horizontal, color, fill, style, coloring, inflate);
        }

        var glow = layout.GlowEnabled && (line.GlowRadius > 0) && (geometry.Count <= MaxGlowPoints) ? GetGlow(line) : null;
        StrokeLine(canvas, line, count, interpolation, horizontal, color, alpha, (float)line.Thickness, glow, style, coloring, inflate);
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

    private void StrokeLine(SKCanvas canvas, LineSeries line, int count, ChartLineInterpolation interpolation, bool horizontal, SKColor color, double alpha, float thickness, SKMaskFilter? glow, ChartStyle style, CartesianLayout? coloring, float inflate)
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
        var dash = GetDash(line, thickness);
        if (glow is not null)
        {
            var glowAlpha = alpha * ChartGlow.Opacity;
            glowPaint.Color = ChartColor.Fade(color, glowAlpha);
            glowPaint.StrokeWidth = thickness * ChartGlow.WidthRatio;
            glowPaint.PathEffect = dash;
            glowPaint.MaskFilter = glow;
            if (coloring is null)
            {
                canvas.DrawPath(path, glowPaint);
            }
            else
            {
                DrawThresholdRegions(canvas, coloring, path, glowPaint, color, glowAlpha, style, inflate);
            }
        }

        var spacing = last > first ? Math.Abs(along[last] - along[first]) / (last - first) : Single.PositiveInfinity;
        var paint = paints.Stroke(ChartColor.Fade(color, alpha), thickness, SKStrokeCap.Round, spacing < thickness ? SKStrokeJoin.Bevel : SKStrokeJoin.Round);
        paint.PathEffect = dash;
        if (coloring is null)
        {
            canvas.DrawPath(path, paint);
        }
        else
        {
            DrawThresholdRegions(canvas, coloring, path, paint, color, alpha, style, inflate);
        }

        bool IsPoint(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]);
    }

    private void FillArea(SKCanvas canvas, AreaSeries area, int count, ChartLineInterpolation interpolation, bool horizontal, SKColor baseColor, double alpha, ChartStyle style, CartesianLayout? coloring, float inflate)
    {
        var color = ChartColor.Fade(baseColor, alpha);
        var opacity = area.FillOpacity;
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
        if (coloring is not null)
        {
            DrawThresholdRegions(canvas, coloring, path, paint, baseColor, alpha * opacity, style, inflate);
            return;
        }

        if (style.FillMode == ChartFillMode.Gradient)
        {
            paint.Color = SKColors.Black;
            paint.Shader = GetGradient(area, fill, extreme, baseline, horizontal);
        }

        canvas.DrawPath(path, paint);

        bool IsFilled(int index) => Single.IsFinite(along[index]) && Single.IsFinite(across[index]) && Single.IsFinite(baseAcross[index]);
    }

    private void DrawBars(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, ChartSeries item, int seriesIndex, SKColor color, ChartStyle style, SelectionBlend selection, double opacity, ChartEntranceAnimation entrance, CartesianRenderState state)
    {
        var geometry = display.GetGeometry(layout, seriesIndex);
        if (geometry.BarSlot < 0)
        {
            return;
        }

        var bar = item as BarSeries;
        var target = layout.Geometries[seriesIndex];
        var axis = layout.GetValueAxis(item.YAxis);
        var negative = bar?.NegativeColor is { } negativeColor ? negativeColor.ToSKColor() : color;
        var radius = (float)GetCornerRadius(item);
        var gap = (float)style.SegmentGap;
        var values = geometry.Value;
        var coloring = IsColoring(layout, state, item.YAxis);
        var gradient = (bar is not null) && (style.FillMode == ChartFillMode.Gradient) && !coloring ? GetBarGradient(bar, layout, geometry, axis, color, negative) : null;
        for (var i = 0; i < geometry.Count; i++)
        {
            if (!TryGetBar(layout, geometry, axis, i, gap, GetGrowth(display, entrance, i, geometry.Count), out var shape))
            {
                continue;
            }

            var index = display.ToTargetIndex(seriesIndex, i);
            var alpha = opacity * selection.PointAlpha(seriesIndex, index, index >= 0 ? target.X[index] : Double.NaN);
            if (gradient?.GetShader(values[i] < 0) is { } shader)
            {
                var paint = paints.Fill(ChartColor.Fade(SKColors.Black, alpha));
                paint.Shader = shader;
                DrawBar(canvas, shape, radius, paint);
                continue;
            }

            var barColor = coloring ? GetThresholdColor(layout, style, values[i], color) : values[i] < 0 ? negative : color;
            DrawBar(canvas, shape, radius, paints.Fill(ChartColor.Fade(barColor, alpha)));
        }
    }

    private void DrawBarTracks(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, ChartStyle style)
    {
        drawnTracks.Clear();
        var plot = layout.PlotBounds;
        var vertical = layout.Orientation == ChartOrientation.Vertical;
        var color = ChartColor.GetTrack(style);
        for (var s = 0; s < series.Count; s++)
        {
            var opacity = display.GetOpacity(s, series[s].IsVisible);
            if (!IsTrackVisible(series[s]) || (opacity <= 0))
            {
                continue;
            }

            var geometry = display.GetGeometry(layout, s);
            if (geometry.BarSlot < 0)
            {
                continue;
            }

            var paint = paints.Fill(ChartColor.Fade(color, opacity));
            for (var i = 0; i < geometry.Count; i++)
            {
                var x = geometry.X[i];
                if (!Double.IsFinite(x) || !drawnTracks.Add((geometry.BarSlot, x)))
                {
                    continue;
                }

                var (from, to) = geometry.GetBarRange(x);
                var along1 = (float)layout.XAxis.ToDisplayPixel(from);
                var along2 = (float)layout.XAxis.ToDisplayPixel(to);
                var rect = vertical
                    ? new SKRect(Math.Min(along1, along2), (float)plot.Y, Math.Max(along1, along2), (float)plot.Bottom)
                    : new SKRect((float)plot.X, Math.Min(along1, along2), (float)plot.Right, Math.Max(along1, along2));
                canvas.DrawRect(rect, paint);
            }
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
        var origin = geometry.IsRange ? (start + end) / 2 : GetBase(axis);
        var (from, to) = geometry.GetBarRange(x);
        var along1 = (float)layout.XAxis.ToDisplayPixel(from);
        var along2 = (float)layout.XAxis.ToDisplayPixel(to);
        var basePoint = (float)axis.ToDisplayPixel(Grow(start, origin, growth));
        var basePixel = Math.Clamp(basePoint, low, high);
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
        shape = new BarShape(rect, edge, outer && endPixel.Equals(dataPixel), geometry.IsRange && basePixel.Equals(basePoint));
        return true;
    }

    private void DrawBar(SKCanvas canvas, in BarShape shape, float radius, SKPaint paint)
    {
        var rect = shape.Rect;
        var vertical = shape.Edge is BarEdge.Top or BarEdge.Bottom;
        var thickness = vertical ? rect.Width : rect.Height;
        var length = vertical ? rect.Height : rect.Width;
        var corner = shape.Rounded || shape.RoundedBase ? Math.Min(radius, Math.Min(thickness / 2, shape.Rounded && shape.RoundedBase ? length / 2 : length)) : 0;
        if (corner <= 0)
        {
            canvas.DrawRect(rect, paint);
            return;
        }

        Span<SKPoint> radii = stackalloc SKPoint[4];
        var point = new SKPoint(corner, corner);
        if (shape.Rounded)
        {
            SetEdgeRadii(radii, shape.Edge, point);
        }

        if (shape.RoundedBase)
        {
            SetEdgeRadii(radii, GetOpposite(shape.Edge), point);
        }

        roundRect.SetRectRadii(rect, radii);
        canvas.DrawRoundRect(roundRect, paint);
    }

    private void DrawMarkers(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, SelectionBlend selection, CartesianRenderState state, float width, float height)
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
            if ((series[s] is not LineSeries line) || (opacity <= 0) || ((line.MarkerMode == ChartMarkerMode.None) && !line.LiveIndicatorVisible))
            {
                continue;
            }

            var radius = (float)line.MarkerSize / 2;
            if (radius <= 0)
            {
                continue;
            }

            var alpha = opacity * selection.SeriesAlpha(s);
            var geometry = display.GetGeometry(layout, s);
            var values = geometry.Value;
            var count = Project(layout, display, geometry, layout.GetValueAxis(line.YAxis), display.GetEntrance(line.Kind));
            if (line.LiveIndicatorVisible)
            {
                DrawLiveIndicator(canvas, layout, line, values, count, radius, knockout, colors[s], alpha, style, state, width, height);
            }

            if (line.MarkerMode == ChartMarkerMode.None)
            {
                continue;
            }

            for (var i = count - 1; i >= 0; i--)
            {
                if (!Single.IsFinite(along[i]) || !Single.IsFinite(across[i]))
                {
                    continue;
                }

                var color = ChartColor.Fade(GetPointColor(layout, state, style, line.YAxis, values[i], colors[s]), alpha);
                DrawMarkerInPlot(canvas, plot, LinePathBuilder.ToPoint(along[i], across[i], horizontal), radius, knockout, line.MarkerShape, color, width, height);
                if ((line.MarkerMode == ChartMarkerMode.Auto) || layout.LastMarkerOnly)
                {
                    break;
                }
            }

            var selected = display.ToDisplayIndex(s, GetSelectedIndex(layout, s, selection.Current));
            if ((selected >= 0) && (selected < count) && Single.IsFinite(along[selected]) && Single.IsFinite(across[selected]))
            {
                var scale = 0.6f + (0.4f * (float)selection.Progress);
                var color = ChartColor.Fade(GetPointColor(layout, state, style, line.YAxis, values[selected], colors[s]), opacity);
                DrawMarkerInPlot(canvas, plot, LinePathBuilder.ToPoint(along[selected], across[selected], horizontal), radius * scale, knockout * scale, line.MarkerShape, color, width, height);
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

    private static bool IsInPlot(ChartRect plot, SKPoint point) =>
        (point.X >= plot.X - EdgeTolerance) && (point.X <= plot.Right + EdgeTolerance) && (point.Y >= plot.Y - EdgeTolerance) && (point.Y <= plot.Bottom + EdgeTolerance);

    private static SKPoint ClampMarker(SKPoint point, float outer, float width, float height) =>
        new(width > outer * 2 ? Math.Clamp(point.X, outer, width - outer) : point.X, height > outer * 2 ? Math.Clamp(point.Y, outer, height - outer) : point.Y);

    private static float GetMarkerExtent(ChartMarkerShape shape, float radius) =>
        shape == ChartMarkerShape.Diamond ? radius * MathF.Sqrt(2) : radius;

    private static void DrawMarkerShape(SKCanvas canvas, SKPoint center, float radius, ChartMarkerShape shape, SKPaint paint)
    {
        switch (shape)
        {
            case ChartMarkerShape.Square:
                canvas.DrawRect(center.X - radius, center.Y - radius, radius * 2, radius * 2, paint);
                break;
            case ChartMarkerShape.Diamond:
                canvas.Save();
                canvas.RotateDegrees(45, center.X, center.Y);
                canvas.DrawRect(center.X - radius, center.Y - radius, radius * 2, radius * 2, paint);
                canvas.Restore();
                break;
            default:
                canvas.DrawCircle(center, radius, paint);
                break;
        }
    }

    private void DrawMarkerInPlot(SKCanvas canvas, ChartRect plot, SKPoint point, float radius, float knockout, ChartMarkerShape shape, SKColor color, float width, float height)
    {
        if (IsInPlot(plot, point))
        {
            DrawMarker(canvas, point, radius, knockout, shape, color, width, height);
        }
    }

    private void DrawLiveIndicator(SKCanvas canvas, CartesianLayout layout, LineSeries line, Span<double> values, int count, float radius, float knockout, SKColor seriesColor, double alpha, ChartStyle style, CartesianRenderState state, float width, float height)
    {
        var last = count - 1;
        while ((last >= 0) && (!Single.IsFinite(along[last]) || !Single.IsFinite(across[last])))
        {
            last--;
        }

        if (last < 0)
        {
            return;
        }

        var point = LinePathBuilder.ToPoint(along[last], across[last], layout.Orientation == ChartOrientation.Horizontal);
        if (!IsInPlot(layout.PlotBounds, point))
        {
            return;
        }

        var color = ChartColor.Fade(GetPointColor(layout, state, style, line.YAxis, values[last], seriesColor), alpha);
        LiveIndicator.DrawRings(canvas, paints, ClampMarker(point, GetMarkerExtent(line.MarkerShape, radius + knockout), width, height), radius, color, state.LivePhase);
        if (line.MarkerMode == ChartMarkerMode.None)
        {
            DrawMarker(canvas, point, radius, knockout, line.MarkerShape, color, width, height);
        }
    }

    private static float GetTrackballPosition(CartesianLayout layout, SnapshotInterpolator display, CartesianInteraction interaction, float scale)
    {
        if (!interaction.Snaps)
        {
            if (!interaction.IsFollowing)
            {
                return GetPointerAlong(layout, interaction, scale);
            }

            return Double.IsFinite(interaction.FollowValue) ? Snap(layout.XAxis.ToDisplayPixel(interaction.FollowValue), scale) : Single.NaN;
        }

        var lead = interaction.LeadSeries;
        if ((lead < 0) || (lead >= layout.Geometries.Count))
        {
            return Single.NaN;
        }

        var geometry = display.GetGeometry(layout, lead);
        var index = display.ToDisplayIndex(lead, interaction.LeadIndex);
        return (index >= 0) && (index < geometry.Count) ? Snap(layout.XAxis.ToDisplayPixel(geometry.X[index]), scale) : Single.NaN;
    }

    private static float GetPointerAlong(CartesianLayout layout, CartesianInteraction interaction, float scale)
    {
        var plot = layout.PlotBounds;
        var point = interaction.TrackPoint;
        return Snap(layout.XAxis.IsVertical ? Math.Clamp(point.Y, plot.Y, plot.Bottom) : Math.Clamp(point.X, plot.X, plot.Right), scale);
    }

    private static float GetPointerAcross(CartesianLayout layout, CartesianInteraction interaction, float scale)
    {
        var plot = layout.PlotBounds;
        var point = interaction.TrackPoint;
        return Snap(layout.XAxis.IsVertical ? Math.Clamp(point.X, plot.X, plot.Right) : Math.Clamp(point.Y, plot.Y, plot.Bottom), scale);
    }

    private void DrawTrackball(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, CartesianRenderState state, float scale, float width, float height)
    {
        if (!interaction.IsTracking)
        {
            return;
        }

        var position = GetTrackballPosition(layout, display, interaction, scale);
        if (Single.IsNaN(position))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var paint = paints.Stroke(style.TrackballLineColor.ToSKColor(), 0, SKStrokeCap.Butt);
        DrawLine(canvas, layout.XAxis.IsVertical, position, plot, paint);
        if (!interaction.Snaps && !interaction.IsFollowing)
        {
            DrawLine(canvas, !layout.XAxis.IsVertical, GetPointerAcross(layout, interaction, scale), plot, paint);
        }

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

            var radius = (float)line.MarkerSize / 2;
            if (radius > 0)
            {
                var point = LinePathBuilder.ToPoint((float)layout.XAxis.ToDisplayPixel(geometry.X[index]), (float)layout.GetValueAxis(line.YAxis).ToDisplayPixel(geometry.End[index]), horizontal);
                DrawMarkerInPlot(canvas, plot, point, radius, knockout, line.MarkerShape, GetPointColor(layout, state, style, line.YAxis, geometry.Value[index], colors[s]), width, height);
            }
        }
    }

    private void DrawTooltip(SKCanvas canvas, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, float width, float height)
    {
        if (!interaction.IsTracking || !interaction.TooltipVisible || (interaction.TooltipHeader is not { } header))
        {
            return;
        }

        ChartTooltipPainter.Draw(canvas, text, paints, header, interaction.TrackedTexts, colors, interaction.TrackPoint, style, width, height);
    }

    private void DrawMarker(SKCanvas canvas, SKPoint point, float radius, float knockout, ChartMarkerShape shape, SKColor color, float width, float height)
    {
        var outer = radius + knockout;
        var center = ClampMarker(point, GetMarkerExtent(shape, outer), width, height);
        if (knockout > 0)
        {
            DrawMarkerShape(canvas, center, outer, shape, paints.Clear);
        }

        DrawMarkerShape(canvas, center, radius, shape, paints.Fill(color));
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
        // ReSharper disable once ForCanBeConvertedToForeach
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
            if (item.Kind is ChartSeriesKind.Bar or ChartSeriesKind.RangeBar)
            {
                if (!TryGetBar(layout, geometry, axis, index, gap, growth, out var shape) || !TryPlaceBarLabel(layout.PlotBounds, geometry.IsStacked, shape, label.Low ? GetOpposite(shape.Edge) : shape.Edge, size, out bounds, out var inside))
                {
                    continue;
                }

                var fill = (geometry.Value[index] < 0) && (item is BarSeries { NegativeColor: { } negative }) ? negative.ToSKColor() : colors[label.SeriesIndex];
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

    private bool TryPlaceBarLabel(ChartRect plot, bool stacked, in BarShape shape, BarEdge edge, SKSize size, out SKRect bounds, out bool inside)
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
        bounds = edge switch
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

        bounds = edge switch
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

        var radius = line.MarkerMode == ChartMarkerMode.None ? 0 : (float)line.MarkerSize / 2;
        var marker = radius > 0 ? radius + (float)style.MarkerKnockout : 0;
        var offset = Math.Max(marker, (float)line.Thickness / 2) + DataLabelOffset;
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
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < axis.Labels.Count; i++)
        {
            var label = axis.Labels[i];
            if (axis.IsDisplayed(label.Value))
            {
                DrawText(canvas, label.Text, axis.GetDisplayBounds(label), axis.Options.LabelFontSize, label.MaxWidth, color, style);
            }
        }
    }

    private void DrawTitle(SKCanvas canvas, CartesianLayout layout, AxisLayout axis, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style)
    {
        if (!axis.ShowsTitle || (axis.Options.Title is not { } title))
        {
            return;
        }

        var rotated = axis.TitleRotation != 0;
        if (rotated)
        {
            canvas.Save();
            canvas.Translate((float)axis.TitleArea.CenterX, (float)axis.TitleArea.CenterY);
            canvas.RotateDegrees((float)axis.TitleRotation);
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

        if (rotated)
        {
            canvas.Restore();
        }
    }

    private void DrawLegend(SKCanvas canvas, LegendLayout legend, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, float scale)
    {
        if (!legend.IsVisible)
        {
            return;
        }

        if (legend.IsTable)
        {
            DrawLegendTable(canvas, legend, style, scale);
        }

        // ReSharper disable once ForCanBeConvertedToForeach
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

    private void DrawLegendTable(SKCanvas canvas, LegendLayout legend, ChartStyle style, float scale)
    {
        var thickness = (float)style.GridLineThickness;
        var paint = paints.Stroke(style.GridLineColor.ToSKColor(), thickness, SKStrokeCap.Butt);
        var snap = thickness <= 0 ? scale : 0;
        DrawSeparator(canvas, legend.Bounds, legend.HeaderBounds.Bottom, paint, snap);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < legend.Items.Count; i++)
        {
            DrawSeparator(canvas, legend.Bounds, legend.Items[i].Bounds.Bottom, paint, snap);
        }

        var color = style.SecondaryTextColor.ToSKColor();
        var sorted = style.PrimaryTextColor.ToSKColor();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < legend.Cells.Count; i++)
        {
            var cell = legend.Cells[i];
            DrawText(canvas, cell.Text, cell.Bounds, style.LegendFontSize, cell.MaxWidth, cell.Sorted ? sorted : color, style);
        }

        if (!legend.SortArrow.IsEmpty)
        {
            DrawSortArrow(canvas, legend.SortArrow, legend.SortDescending, paints.Stroke(sorted, SortArrowThickness, SKStrokeCap.Square));
        }
    }

    private static void DrawSortArrow(SKCanvas canvas, ChartRect bounds, bool descending, SKPaint paint)
    {
        var tip = (float)(descending ? bounds.Bottom : bounds.Y);
        var ends = (float)(descending ? bounds.Y : bounds.Bottom);
        var center = (float)bounds.CenterX;
        canvas.DrawLine((float)bounds.X, ends, center, tip, paint);
        canvas.DrawLine(center, tip, (float)bounds.Right, ends, paint);
    }

    private static void DrawSeparator(SKCanvas canvas, ChartRect bounds, double bottom, SKPaint paint, float snap)
    {
        var y = bottom - (LegendLayout.SeparatorThickness / 2);
        var position = snap > 0 ? Snap(y, snap) : (float)y;
        canvas.DrawLine((float)bounds.X, position, (float)bounds.Right, position, paint);
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

    private SKMaskFilter GetGlow(LineSeries line)
    {
        if (!glows.TryGetValue(line, out var entry))
        {
            entry = new GlowEntry();
            glows.Add(line, entry);
        }

        entry.Frame = frame;
        var sigma = ChartGlow.GetSigma(line.GlowRadius);
        if ((entry.Filter is null) || !entry.Sigma.Equals(sigma))
        {
            entry.Filter?.Dispose();
            entry.Filter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            entry.Sigma = sigma;
        }

        return entry.Filter;
    }

    private SKShader GetGradient(AreaSeries area, SKColor color, float from, float to, bool horizontal)
    {
        if (!gradients.TryGetValue(area, out var entry))
        {
            entry = new GradientEntry();
            gradients.Add(area, entry);
        }

        entry.Frame = frame;
        return entry.Update(color, from, to, horizontal);
    }

    private BarGradientEntry GetBarGradient(BarSeries bar, CartesianLayout layout, SeriesGeometry geometry, AxisLayout axis, SKColor color, SKColor negative)
    {
        if (!barGradients.TryGetValue(bar, out var entry))
        {
            entry = new BarGradientEntry();
            barGradients.Add(bar, entry);
        }

        entry.Frame = frame;
        var origin = GetBase(axis);
        var high = origin;
        var low = origin;
        var end = geometry.End;
        for (var i = 0; i < geometry.Count; i++)
        {
            if (Double.IsFinite(end[i]))
            {
                high = Math.Max(high, end[i]);
                low = Math.Min(low, end[i]);
            }
        }

        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var basePixel = (float)axis.ToDisplayPixel(origin);
        entry.HasPositive = high > origin;
        entry.HasNegative = low < origin;
        if (entry.HasPositive)
        {
            entry.Positive.Update(color, (float)axis.ToDisplayPixel(high), basePixel, horizontal, BarGradientTail);
        }

        if (entry.HasNegative)
        {
            entry.Negative.Update(negative, (float)axis.ToDisplayPixel(low), basePixel, horizontal, BarGradientTail);
        }

        return entry;
    }

    private void RemoveStaleEntries()
    {
        RemoveStaleReferenceDashes();
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

        staleGlows.Clear();
        foreach (var (line, entry) in glows)
        {
            if (entry.Frame != frame)
            {
                entry.Filter?.Dispose();
                staleGlows.Add(line);
            }
        }

        foreach (var line in staleGlows)
        {
            glows.Remove(line);
        }

        staleBarGradients.Clear();
        foreach (var (bar, entry) in barGradients)
        {
            if (entry.Frame != frame)
            {
                entry.Dispose();
                staleBarGradients.Add(bar);
            }
        }

        foreach (var bar in staleBarGradients)
        {
            barGradients.Remove(bar);
        }
    }

    private sealed class DashEntry
    {
        public float[] Intervals { get; set; } = [];

        public SKPathEffect? Effect { get; set; }

        public int Frame { get; set; }
    }

    private sealed class GlowEntry
    {
        public SKMaskFilter? Filter { get; set; }

        public float Sigma { get; set; }

        public int Frame { get; set; }
    }

    private sealed class BarGradientEntry : IDisposable
    {
        public GradientEntry Positive { get; } = new();

        public GradientEntry Negative { get; } = new();

        public bool HasPositive { get; set; }

        public bool HasNegative { get; set; }

        public int Frame { get; set; }

        public SKShader? GetShader(bool negative) =>
            negative ? HasNegative ? Negative.Shader : null : HasPositive ? Positive.Shader : null;

        public void Dispose()
        {
            Positive.Shader?.Dispose();
            Negative.Shader?.Dispose();
        }
    }
}
