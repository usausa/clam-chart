namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed partial class CartesianRenderer
{
    private const float ThresholdLineWidth = 2;

    private const float ThresholdDash = 10;

    private const double ThresholdLineOpacity = 0.7;

    private const double BandOpacity = 0.15;

    private const float ReferenceLineWidth = 1;

    private const float ReferenceDash = 4;

    private const float AnnotationLabelGap = 2;

    private const float ScatterStrokeWidth = 1;

    private const double ScatterFillOpacity = 0.5;

    private const float FadeStartScale = 0.6f;

    private readonly SKPathEffect thresholdDash = SKPathEffect.CreateDash([ThresholdDash, ThresholdDash], 0);
    private readonly SKPathEffect referenceDash = SKPathEffect.CreateDash([ReferenceDash, ReferenceDash], 0);
    private readonly Dictionary<ChartReferenceLine, DashEntry> referenceDashes = [];
    private readonly List<ChartReferenceLine> staleReferenceDashes = [];
    private readonly List<SKColor> tooltipColors = [];

    private static bool HasMode(CartesianRenderState state, ChartThresholdsMode mode) => (state.ThresholdsMode & mode) != 0;

    private static bool IsColoring(CartesianLayout layout, CartesianRenderState state, ChartAxisTarget target) =>
        HasMode(state, ChartThresholdsMode.ColorSeries) && (layout.Thresholds.Count > 0) && (layout.GetValueAxis(target) == layout.YAxis);

    private static SKColor GetThresholdColor(CartesianLayout layout, ChartStyle style, double value, SKColor color) =>
        ThresholdResolver.GetColor(ThresholdResolver.ResolveSorted(layout.Thresholds, value), style)?.ToSKColor() ?? color;

    private static SKColor GetPointColor(CartesianLayout layout, CartesianRenderState state, ChartStyle style, ChartAxisTarget target, double value, SKColor color) =>
        IsColoring(layout, state, target) ? GetThresholdColor(layout, style, value, color) : color;

    private static float ToSpanPixel(AxisLayout axis, double value)
    {
        var scale = axis.DisplayScale;
        var span = Math.Abs(scale.Span);
        return (float)axis.ToDisplayPixel(Math.Clamp(value, Math.Min(scale.Minimum, scale.Maximum) - span, Math.Max(scale.Minimum, scale.Maximum) + span));
    }

    private static SKRect GetSpanRect(AxisLayout axis, ChartRect plot, double from, double to, float inflate)
    {
        var first = ToSpanPixel(axis, from);
        var second = ToSpanPixel(axis, to);
        var low = Math.Min(first, second);
        var high = Math.Max(first, second);
        return axis.IsVertical
            ? new SKRect((float)plot.X - inflate, low, (float)plot.Right + inflate, high)
            : new SKRect(low, (float)plot.Y - inflate, high, (float)plot.Bottom + inflate);
    }

    private void DrawBands(SKCanvas canvas, CartesianLayout layout, CartesianRenderState state, ChartStyle style)
    {
        var plot = layout.PlotBounds;
        if (plot.IsEmpty)
        {
            return;
        }

        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)plot.X, (float)plot.Y, (float)plot.Width, (float)plot.Height));
        if (HasMode(state, ChartThresholdsMode.Bands))
        {
            var thresholds = layout.Thresholds;
            for (var i = 0; i < thresholds.Count; i++)
            {
                if (ThresholdResolver.GetColor(thresholds[i], style) is not { } color)
                {
                    continue;
                }

                var to = i + 1 < thresholds.Count ? thresholds[i + 1].Value : Double.PositiveInfinity;
                canvas.DrawRect(GetSpanRect(layout.YAxis, plot, thresholds[i].Value, to, 0), paints.Fill(ChartColor.Fade(color.ToSKColor(), BandOpacity)));
            }
        }

        var annotations = layout.Annotations;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < annotations.Count; i++)
        {
            var annotation = annotations[i];
            if (!annotation.IsBand || (state.Annotations[annotation.Index] is not ChartReferenceBand band))
            {
                continue;
            }

            var axis = annotation.Axis == ChartAnnotationAxis.X ? layout.XAxis : layout.GetValueAxis(annotation.Target);
            var color = (band.Color ?? style.MutedTextColor).ToSKColor();
            canvas.DrawRect(GetSpanRect(axis, plot, annotation.From, annotation.To, 0), paints.Fill(ChartColor.Fade(color, BandOpacity)));
        }

        canvas.Restore();
    }

    private void DrawThresholdLines(SKCanvas canvas, CartesianLayout layout, CartesianRenderState state, ChartStyle style)
    {
        if (!HasMode(state, ChartThresholdsMode.Lines))
        {
            return;
        }

        var axis = layout.YAxis;
        var thresholds = layout.Thresholds;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < thresholds.Count; i++)
        {
            var threshold = thresholds[i];
            if (!Double.IsFinite(threshold.Value) || !axis.IsDisplayed(threshold.Value) || (ThresholdResolver.GetColor(threshold, style) is not { } color))
            {
                continue;
            }

            var paint = paints.Stroke(ChartColor.Fade(color.ToSKColor(), ThresholdLineOpacity), ThresholdLineWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter);
            paint.PathEffect = thresholdDash;
            DrawLine(canvas, axis.IsVertical, (float)axis.ToDisplayPixel(threshold.Value), layout.PlotBounds, paint);
        }
    }

    private void DrawReferenceLines(SKCanvas canvas, CartesianLayout layout, CartesianRenderState state, ChartStyle style)
    {
        var annotations = layout.Annotations;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < annotations.Count; i++)
        {
            var annotation = annotations[i];
            if (annotation.IsBand || (state.Annotations[annotation.Index] is not ChartReferenceLine line))
            {
                continue;
            }

            var axis = annotation.Axis == ChartAnnotationAxis.X ? layout.XAxis : layout.GetValueAxis(annotation.Target);
            if (!axis.IsDisplayed(annotation.From))
            {
                continue;
            }

            var paint = paints.Stroke((line.Color ?? style.SecondaryTextColor).ToSKColor(), ReferenceLineWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter);
            paint.PathEffect = GetReferenceDash(line);
            DrawLine(canvas, axis.IsVertical, (float)axis.ToDisplayPixel(annotation.From), layout.PlotBounds, paint);
        }
    }

    private void DrawAnnotationLabels(SKCanvas canvas, CartesianLayout layout, CartesianRenderState state, ChartStyle style)
    {
        if (HasMode(state, ChartThresholdsMode.Lines))
        {
            var thresholds = layout.Thresholds;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < thresholds.Count; i++)
            {
                var threshold = thresholds[i];
                if (String.IsNullOrEmpty(threshold.Label) || !Double.IsFinite(threshold.Value) || !layout.YAxis.IsDisplayed(threshold.Value) || (ThresholdResolver.GetColor(threshold, style) is not { } color))
                {
                    continue;
                }

                DrawLineLabel(canvas, layout, layout.YAxis, threshold.Value, threshold.Label, color.ToSKColor(), style);
            }
        }

        var annotations = layout.Annotations;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < annotations.Count; i++)
        {
            var annotation = annotations[i];
            var source = state.Annotations[annotation.Index];
            if (String.IsNullOrEmpty(source.Label))
            {
                continue;
            }

            var axis = annotation.Axis == ChartAnnotationAxis.X ? layout.XAxis : layout.GetValueAxis(annotation.Target);
            if (annotation.IsBand)
            {
                DrawBandLabel(canvas, layout, axis, annotation, source.Label, (source.Color ?? style.SecondaryTextColor).ToSKColor(), style);
            }
            else if (axis.IsDisplayed(annotation.From))
            {
                DrawLineLabel(canvas, layout, axis, annotation.From, source.Label, (source.Color ?? style.SecondaryTextColor).ToSKColor(), style);
            }
        }
    }

    private void DrawLineLabel(SKCanvas canvas, CartesianLayout layout, AxisLayout axis, double value, string label, SKColor color, ChartStyle style)
    {
        var plot = layout.PlotBounds;
        var size = style.DataLabelFontSize;
        var width = (float)text.Measure(label, size, Double.PositiveInfinity);
        var height = (float)text.GetLineHeight(size);
        var position = (float)axis.ToDisplayPixel(value);
        var bounds = axis.IsVertical
            ? new ChartRect(plot.Right - AnnotationLabelGap - width, position - AnnotationLabelGap - height, width, height)
            : new ChartRect(position + AnnotationLabelGap, plot.Y + AnnotationLabelGap, width, height);
        DrawText(canvas, label, bounds, size, Double.PositiveInfinity, color, style);
    }

    private void DrawBandLabel(SKCanvas canvas, CartesianLayout layout, AxisLayout axis, CartesianAnnotation annotation, string label, SKColor color, ChartStyle style)
    {
        var plot = layout.PlotBounds;
        var rect = GetSpanRect(axis, plot, annotation.From, annotation.To, 0);
        var left = Math.Max(rect.Left, (float)plot.X);
        var top = Math.Max(rect.Top, (float)plot.Y);
        var right = Math.Min(rect.Right, (float)plot.Right);
        var bottom = Math.Min(rect.Bottom, (float)plot.Bottom);
        if ((right <= left) || (bottom <= top))
        {
            return;
        }

        var size = style.DataLabelFontSize;
        var height = (float)text.GetLineHeight(size);
        var bounds = new ChartRect(left + AnnotationLabelGap, top + AnnotationLabelGap, Math.Max(0, right - left - (AnnotationLabelGap * 2)), height);
        DrawText(canvas, label, bounds, size, bounds.Width, color, style);
    }

    private static void DrawThresholdRegions(SKCanvas canvas, CartesianLayout layout, SKPath path, SKPaint paint, SKColor color, double alpha, ChartStyle style, float inflate)
    {
        var thresholds = layout.Thresholds;
        var axis = layout.YAxis;
        var plot = layout.PlotBounds;
        var first = Double.IsNegativeInfinity(thresholds[0].Value) ? 1 : 0;
        var regionColor = first == 1 ? ThresholdResolver.GetColor(thresholds[0], style)?.ToSKColor() ?? color : color;
        var start = Double.NegativeInfinity;
        for (var i = first; i <= thresholds.Count; i++)
        {
            var end = i < thresholds.Count ? thresholds[i].Value : Double.PositiveInfinity;
            var rect = GetSpanRect(axis, plot, start, end, inflate);
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

    private void DrawScatter(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, ScatterSeries scatter, int seriesIndex, SKColor color, ChartStyle style, SelectionBlend selection, double opacity, ChartEntranceAnimation entrance, CartesianRenderState state)
    {
        var geometry = display.GetGeometry(layout, seriesIndex);
        var target = layout.Geometries[seriesIndex];
        var count = Project(layout, display, geometry, layout.GetValueAxis(scatter.YAxis), entrance);
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var scale = entrance == ChartEntranceAnimation.Fade ? FadeStartScale + ((1 - FadeStartScale) * (float)display.Ease(display.Progress)) : 1;
        var radius = (float)(scatter.MarkerSize ?? style.MarkerSize) * scale / 2;
        var fill = scatter.FillOpacity ?? ScatterFillOpacity;
        var coloring = IsColoring(layout, state, scatter.YAxis);
        var values = geometry.Value;
        var sizes = geometry.Size;
        var bubbles = scatter is BubbleSeries;
        if (bubbles)
        {
            SortBySize(sizes, count);
        }

        for (var k = 0; k < count; k++)
        {
            var i = bubbles ? drawOrder[k] : k;
            if (!Single.IsFinite(along[i]) || !Single.IsFinite(across[i]))
            {
                continue;
            }

            var index = display.ToTargetIndex(seriesIndex, i);
            var alpha = opacity * selection.PointAlpha(seriesIndex, index, index >= 0 ? target.X[index] : Double.NaN);
            if (alpha <= 0)
            {
                continue;
            }

            var pointColor = coloring ? GetThresholdColor(layout, style, values[i], color) : color;
            var pointRadius = bubbles ? (float)sizes[i] * scale / 2 : radius;
            DrawScatterPoint(canvas, LinePathBuilder.ToPoint(along[i], across[i], horizontal), pointRadius, scatter.MarkerShape, ChartColor.Fade(pointColor, alpha * fill), ChartColor.Fade(pointColor, alpha));
        }
    }

    private void SortBySize(Span<double> sizes, int count)
    {
        Ensure(ref orderKeys, count);
        Ensure(ref drawOrder, count);
        for (var i = 0; i < count; i++)
        {
            orderKeys[i] = -(float)sizes[i];
            drawOrder[i] = i;
        }

        Array.Sort(orderKeys, drawOrder, 0, count);
    }

    private void DrawScatterPoint(SKCanvas canvas, SKPoint point, float radius, ChartMarkerShape shape, SKColor fill, SKColor stroke)
    {
        if (!(radius > 0))
        {
            return;
        }

        DrawMarkerShape(canvas, point, radius, shape, paints.Fill(fill));
        DrawMarkerShape(canvas, point, radius, shape, paints.Stroke(stroke, ScatterStrokeWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter));
    }

    private void DrawCrosshair(SKCanvas canvas, CartesianLayout layout, SnapshotInterpolator display, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, CartesianRenderState state, float scale, float width, float height)
    {
        var lead = interaction.LeadSeries;
        if ((lead < 0) || (lead >= series.Count) || (lead >= layout.Geometries.Count))
        {
            return;
        }

        var geometry = display.GetGeometry(layout, lead);
        var index = display.ToDisplayIndex(lead, interaction.LeadIndex);
        if ((index < 0) || (index >= geometry.Count) || !Double.IsFinite(geometry.X[index]) || !Double.IsFinite(geometry.End[index]))
        {
            return;
        }

        var plot = layout.PlotBounds;
        var item = series[lead];
        var axis = layout.GetValueAxis(item.YAxis);
        var paint = paints.Stroke(style.TrackballLineColor.ToSKColor(), 0, SKStrokeCap.Butt);
        DrawLine(canvas, layout.XAxis.IsVertical, Snap(layout.XAxis.ToDisplayPixel(geometry.X[index]), scale), plot, paint);
        DrawLine(canvas, axis.IsVertical, Snap(axis.ToDisplayPixel(geometry.End[index]), scale), plot, paint);
        var scatter = item as ScatterSeries;
        var size = scatter?.MarkerSize ?? style.MarkerSize;
        var shape = scatter?.MarkerShape ?? ChartMarkerShape.Circle;
        var point = LinePathBuilder.ToPoint((float)layout.XAxis.ToDisplayPixel(geometry.X[index]), (float)axis.ToDisplayPixel(geometry.End[index]), layout.Orientation == ChartOrientation.Horizontal);
        var color = GetPointColor(layout, state, style, item.YAxis, geometry.Value[index], colors[lead]);
        DrawMarkerInPlot(canvas, plot, point, (float)size / 2, (float)style.MarkerKnockout, shape, color, width, height);
    }

    private List<SKColor> GetTooltipColors(CartesianLayout layout, IReadOnlyList<ChartSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style, CartesianInteraction interaction, CartesianRenderState state)
    {
        tooltipColors.Clear();
        var indices = interaction.TrackedIndices;
        for (var s = 0; s < colors.Count; s++)
        {
            var color = colors[s];
            if ((s < indices.Count) && (indices[s] >= 0) && (s < series.Count) && (s < layout.Geometries.Count) && (indices[s] < layout.Geometries[s].Count))
            {
                color = GetPointColor(layout, state, style, series[s].YAxis, layout.Geometries[s].Value[indices[s]], color);
            }

            tooltipColors.Add(color);
        }

        return tooltipColors;
    }

    private SKPathEffect? GetReferenceDash(ChartReferenceLine line)
    {
        var pattern = line.DashPattern;
        if (pattern is null)
        {
            return referenceDash;
        }

        if (pattern.Count == 0)
        {
            return null;
        }

        if (!referenceDashes.TryGetValue(line, out var entry))
        {
            entry = new DashEntry();
            referenceDashes.Add(line, entry);
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
            var value = (float)Math.Max(0, pattern[i % pattern.Count]) * ReferenceLineWidth;
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

    private void RemoveStaleReferenceDashes()
    {
        staleReferenceDashes.Clear();
        foreach (var (line, entry) in referenceDashes)
        {
            if (entry.Frame != frame)
            {
                entry.Effect?.Dispose();
                staleReferenceDashes.Add(line);
            }
        }

        foreach (var line in staleReferenceDashes)
        {
            referenceDashes.Remove(line);
        }
    }

    private void DisposeOverlays()
    {
        thresholdDash.Dispose();
        referenceDash.Dispose();
        foreach (var entry in referenceDashes.Values)
        {
            entry.Effect?.Dispose();
        }

        referenceDashes.Clear();
    }
}
