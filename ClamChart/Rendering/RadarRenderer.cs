namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class RadarRenderer : IDisposable
{
    private const float VertexMarkerSize = 8;

    private const double Tolerance = 1e-9;

    private const float SelectedMarkerStart = 0.6f;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly List<SKPath?> paths = [];
    private SKPath? gridPath;
    private SKPath? revealPath;
    private int layoutVersion = -1;
    private int displayVersion = -1;

    public IChartTextMeasurer Measurer => text;

    public RadarRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
        ReleasePaths();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, RadarLayout layout, RadarInterpolator display, IReadOnlyList<RadarSeries> series, IReadOnlyList<SKColor> colors, RadarInteraction interaction, ChartStyle style, float width, float height)
    {
        EnsurePaths(layout, display, series.Count);
        var progress = interaction.SelectionProgress < 1 ? display.Ease(interaction.SelectionProgress) : 1;
        var current = interaction.HasSelection && !interaction.CategorySelection ? interaction.SelectedSeries : -1;
        var previous = (progress < 1) && !interaction.PreviousCategory ? interaction.PreviousSeries : -1;
        var selected = interaction.HasSelection && (interaction.SelectedSeries < layout.SeriesCount) ? layout.FindAxis(interaction.SelectedSeries, interaction.SelectedIndex) : -1;
        DrawGrid(canvas, style);
        ClearLevels(canvas, layout, display);
        DrawAxisLabels(canvas, layout, interaction.CategorySelection ? selected : -1, style);
        DrawSeries(canvas, display, series, colors, new RadarSelection(previous, current, progress, style.DimmedOpacity));
        DrawLevels(canvas, layout, display, style);
        DrawTrackedAxis(canvas, layout, interaction, style);
        DrawMarkers(canvas, layout, display, series, colors, interaction, selected, (float)progress, style);
        DrawLegend(canvas, layout, series, colors, style);
        DrawTooltip(canvas, colors, interaction, style, width, height);
    }

    private static bool TryGetLevelBounds(RadarLayout layout, RadarInterpolator display, RadarLevel level, out ChartRect bounds)
    {
        var scale = display.Scale;
        var ratio = scale.Span > 0 ? (level.Value - scale.Minimum) / scale.Span : 0;
        if ((ratio <= Tolerance) || (ratio > 1 + Tolerance))
        {
            bounds = default;
            return false;
        }

        var height = layout.LabelHeight;
        bounds = new ChartRect(layout.CenterX - (level.Width / 2), layout.CenterY - (layout.Radius * ratio) - (height / 2), level.Width, height);
        return true;
    }

    private static double GetRingRatio(double value, AxisScale scale)
    {
        var ratio = scale.Span > 0 ? (value - scale.Minimum) / scale.Span : 0;
        return (ratio > Tolerance) && (ratio < 1 - Tolerance) ? ratio : 0;
    }

    private void EnsurePaths(RadarLayout layout, RadarInterpolator display, int count)
    {
        if ((layout.Version == layoutVersion) && (display.Version == displayVersion) && (paths.Count == count))
        {
            return;
        }

        ReleasePaths();
        layoutVersion = layout.Version;
        displayVersion = display.Version;
        if (layout.Radius <= 0)
        {
            for (var s = 0; s < count; s++)
            {
                paths.Add(null);
            }

            return;
        }

        gridPath = BuildGrid(layout, display);
        for (var s = 0; s < count; s++)
        {
            paths.Add(BuildSeries(layout, display, s));
        }

        if (display.RevealAngle < 360)
        {
            revealPath = BuildReveal(layout, display.RevealAngle);
        }
    }

    private SKPath BuildGrid(RadarLayout layout, RadarInterpolator display)
    {
        var builder = paints.Path;
        var ticks = layout.Ticks;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < ticks.Count; i++)
        {
            var ratio = GetRingRatio(ticks[i], display.Scale);
            if (ratio > 0)
            {
                AddRing(builder, layout, layout.Radius * ratio);
            }
        }

        AddRing(builder, layout, layout.Radius);
        var x = (float)layout.CenterX;
        var y = (float)layout.CenterY;
        for (var k = 0; k < layout.AxisCount; k++)
        {
            var (endX, endY) = layout.GetPoint(k, 1);
            builder.MoveTo(x, y);
            builder.LineTo((float)endX, (float)endY);
        }

        return builder.Detach();
    }

    private static void AddRing(SKPathBuilder builder, RadarLayout layout, double radius)
    {
        if (layout.IsCircle)
        {
            builder.AddCircle((float)layout.CenterX, (float)layout.CenterY, (float)radius);
            return;
        }

        for (var k = 0; k < layout.AxisCount; k++)
        {
            var ratio = radius / layout.Radius;
            var (x, y) = layout.GetPoint(k, ratio);
            if (k == 0)
            {
                builder.MoveTo((float)x, (float)y);
            }
            else
            {
                builder.LineTo((float)x, (float)y);
            }
        }

        builder.Close();
    }

    private SKPath? BuildSeries(RadarLayout layout, RadarInterpolator display, int seriesIndex)
    {
        var builder = paints.Path;
        var growth = display.Growth;
        var points = 0;
        for (var k = 0; k < layout.AxisCount; k++)
        {
            var value = display.GetValue(seriesIndex, k);
            if (!Double.IsFinite(value))
            {
                continue;
            }

            var (x, y) = layout.GetPoint(k, RadarLayout.GetRatio(value, display.Scale) * growth);
            if (points == 0)
            {
                builder.MoveTo((float)x, (float)y);
            }
            else
            {
                builder.LineTo((float)x, (float)y);
            }

            points++;
        }

        if (points == 0)
        {
            return null;
        }

        builder.Close();
        return builder.Detach();
    }

    private SKPath BuildReveal(RadarLayout layout, double angle)
    {
        var builder = paints.Path;
        var x = (float)layout.CenterX;
        var y = (float)layout.CenterY;
        var radius = (float)(layout.Radius * 2);
        builder.MoveTo(x, y);
        builder.ArcTo(new SKRect(x - radius, y - radius, x + radius, y + radius), -90, (float)angle, false);
        builder.Close();
        return builder.Detach();
    }

    private void ReleasePaths()
    {
        foreach (var path in paths)
        {
            path?.Dispose();
        }

        paths.Clear();
        gridPath?.Dispose();
        gridPath = null;
        revealPath?.Dispose();
        revealPath = null;
    }

    private void DrawGrid(SKCanvas canvas, ChartStyle style)
    {
        if (gridPath is not null)
        {
            canvas.DrawPath(gridPath, paints.Stroke(style.GridLineColor.ToSKColor(), (float)style.GridLineThickness, SKStrokeCap.Butt, SKStrokeJoin.Miter));
        }
    }

    private void ClearLevels(SKCanvas canvas, RadarLayout layout, RadarInterpolator display)
    {
        var levels = layout.Levels;
        var padding = (float)RadarLayout.LevelLabelPadding;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < levels.Count; i++)
        {
            if (TryGetLevelBounds(layout, display, levels[i], out var bounds))
            {
                canvas.DrawRect((float)bounds.X - padding, (float)bounds.Y - padding, (float)bounds.Width + (padding * 2), (float)bounds.Height + (padding * 2), paints.Clear);
            }
        }
    }

    private void DrawLevels(SKCanvas canvas, RadarLayout layout, RadarInterpolator display, ChartStyle style)
    {
        var levels = layout.Levels;
        var color = style.MutedTextColor.ToSKColor();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < levels.Count; i++)
        {
            if (TryGetLevelBounds(layout, display, levels[i], out var bounds))
            {
                text.DrawText(canvas, levels[i].Text, bounds, style.AxisLabelFontSize, Double.PositiveInfinity, color, style);
            }
        }
    }

    private void DrawAxisLabels(SKCanvas canvas, RadarLayout layout, int selected, ChartStyle style)
    {
        if (!layout.ShowsLabels)
        {
            return;
        }

        var color = style.SecondaryTextColor.ToSKColor();
        var emphasis = style.PrimaryTextColor.ToSKColor();
        var axes = layout.Axes;
        for (var k = 0; k < axes.Count; k++)
        {
            var axis = axes[k];
            if (axis.Text is { } label)
            {
                text.DrawText(canvas, label, axis.LabelBounds, style.AxisLabelFontSize, axis.LabelMaxWidth, k == selected ? emphasis : color, style);
            }
        }
    }

    private void DrawSeries(SKCanvas canvas, RadarInterpolator display, IReadOnlyList<RadarSeries> series, IReadOnlyList<SKColor> colors, RadarSelection selection)
    {
        var entrance = display.EntranceAlpha;
        var clip = revealPath;
        if (clip is not null)
        {
            canvas.Save();
            canvas.ClipPath(clip, SKClipOperation.Intersect, true);
        }

        for (var s = 0; (s < paths.Count) && (s < series.Count); s++)
        {
            var alpha = display.GetOpacity(s) * entrance * selection.Alpha(s);
            if ((paths[s] is { } path) && (alpha > 0) && (series[s].FillOpacity > 0))
            {
                canvas.DrawPath(path, paints.Fill(ChartColor.Fade(colors[s], alpha * series[s].FillOpacity)));
            }
        }

        for (var s = 0; (s < paths.Count) && (s < series.Count); s++)
        {
            var alpha = display.GetOpacity(s) * entrance * selection.Alpha(s);
            var thickness = (float)series[s].Thickness;
            if ((paths[s] is { } path) && (alpha > 0) && (thickness > 0))
            {
                canvas.DrawPath(path, paints.Stroke(ChartColor.Fade(colors[s], alpha), thickness));
            }
        }

        if (clip is not null)
        {
            canvas.Restore();
        }
    }

    private void DrawTrackedAxis(SKCanvas canvas, RadarLayout layout, RadarInteraction interaction, ChartStyle style)
    {
        var axis = interaction.TrackedAxis;
        if (!interaction.IsTracking || (axis < 0) || (axis >= layout.AxisCount) || (layout.Radius <= 0))
        {
            return;
        }

        var (x, y) = layout.GetPoint(axis, 1);
        canvas.DrawLine((float)layout.CenterX, (float)layout.CenterY, (float)x, (float)y, paints.Stroke(style.TrackballLineColor.ToSKColor(), 0, SKStrokeCap.Butt));
    }

    private void DrawMarkers(SKCanvas canvas, RadarLayout layout, RadarInterpolator display, IReadOnlyList<RadarSeries> series, IReadOnlyList<SKColor> colors, RadarInteraction interaction, int selected, float progress, ChartStyle style)
    {
        var overlay = display.OverlayAlpha * display.EntranceAlpha;
        var radius = VertexMarkerSize / 2;
        var knockout = (float)style.MarkerKnockout;
        if ((overlay <= 0) || (radius <= 0))
        {
            return;
        }

        if (selected >= 0)
        {
            var size = SelectedMarkerStart + ((1 - SelectedMarkerStart) * progress);
            for (var s = 0; s < series.Count; s++)
            {
                if (interaction.CategorySelection || (s == interaction.SelectedSeries))
                {
                    DrawMarker(canvas, layout, display, s, selected, radius * size, knockout * size, colors[s], overlay);
                }
            }
        }

        var axis = interaction.TrackedAxis;
        if (!interaction.IsTracking || (axis < 0) || (axis >= layout.AxisCount))
        {
            return;
        }

        var indices = interaction.TrackedIndices;
        for (var s = 0; (s < indices.Count) && (s < series.Count); s++)
        {
            if (indices[s] >= 0)
            {
                DrawMarker(canvas, layout, display, s, axis, radius, knockout, colors[s], overlay);
            }
        }
    }

    private void DrawMarker(SKCanvas canvas, RadarLayout layout, RadarInterpolator display, int seriesIndex, int axis, float radius, float knockout, SKColor color, double overlay)
    {
        var alpha = display.GetOpacity(seriesIndex) * overlay;
        var value = display.GetValue(seriesIndex, axis);
        if ((alpha <= 0) || !Double.IsFinite(value))
        {
            return;
        }

        var (x, y) = layout.GetPoint(axis, RadarLayout.GetRatio(value, display.Scale) * display.Growth);
        var center = new SKPoint((float)x, (float)y);
        if (knockout > 0)
        {
            canvas.DrawCircle(center, radius + knockout, paints.Clear);
        }

        canvas.DrawCircle(center, radius, paints.Fill(ChartColor.Fade(color, alpha)));
    }

    private void DrawLegend(SKCanvas canvas, RadarLayout layout, IReadOnlyList<RadarSeries> series, IReadOnlyList<SKColor> colors, ChartStyle style)
    {
        var legend = layout.Legend;
        if (!legend.IsVisible)
        {
            return;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < legend.Items.Count; i++)
        {
            var item = legend.Items[i];
            if (item.SeriesIndex >= series.Count)
            {
                continue;
            }

            var source = series[item.SeriesIndex];
            var marker = item.Marker;
            var color = source.IsVisible ? colors[item.SeriesIndex] : ChartColor.Fade(colors[item.SeriesIndex], style.DimmedOpacity);
            canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Height / 2, paints.Fill(color));
            var textColor = source.IsVisible ? style.SecondaryTextColor : style.MutedTextColor;
            text.DrawText(canvas, source.Title ?? String.Empty, item.Text, style.LegendFontSize, item.TextMaxWidth, textColor.ToSKColor(), style);
        }

        if (legend.OverflowCount > 0)
        {
            text.DrawText(canvas, legend.OverflowText, legend.OverflowBounds, style.LegendFontSize, Double.PositiveInfinity, style.MutedTextColor.ToSKColor(), style);
        }
    }

    private void DrawTooltip(SKCanvas canvas, IReadOnlyList<SKColor> colors, RadarInteraction interaction, ChartStyle style, float width, float height)
    {
        if (!interaction.IsTracking || !interaction.TooltipVisible || (interaction.TooltipHeader is not { } header))
        {
            return;
        }

        ChartTooltipPainter.Draw(canvas, text, paints, header, interaction.TrackedTexts, colors, interaction.TrackPoint, style, width, height);
    }
}
