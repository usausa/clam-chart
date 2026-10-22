namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class StateTimelineRenderer : IDisposable
{
    private const float TextPadding = 2;

    private const int MinCharacters = 2;

    private const byte HoverAlpha = 51;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly ChartSpanText spans = new();

    public IChartTextMeasurer Measurer => text;

    public StateTimelineRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, StateTimelineLayout layout, StateTimelineInterpolator display, StateTimelineInteraction interaction, StateTimelineRenderState state, ChartStyle style, float width, float height)
    {
        var scale = canvas.TotalMatrix.ScaleX;
        DrawGrid(canvas, layout, style);
        DrawSegments(canvas, layout, display, interaction, state, style, scale);
        DrawLabels(canvas, layout, style);
        DrawCursor(canvas, layout, interaction, style);
        DrawLegend(canvas, layout, style);
        DrawTooltip(canvas, interaction, style, width, height);
    }

    private static float Snap(double value, float scale) => scale > 0 ? MathF.Round((float)value * scale) / scale : (float)value;

    private static SKColor GetFill(SKColor color, double opacity) => color.Alpha < 255 ? color : ChartColor.Fade(color, opacity);

    private void DrawGrid(SKCanvas canvas, StateTimelineLayout layout, ChartStyle style)
    {
        var plot = layout.Plot;
        var axis = layout.Axis;
        var paint = paints.Stroke(style.GridLineColor.ToSKColor(), (float)style.GridLineThickness, SKStrokeCap.Butt);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < axis.Ticks.Count; i++)
        {
            var tick = axis.Ticks[i];
            if (axis.IsDisplayed(tick))
            {
                var x = (float)axis.ToDisplayPixel(tick);
                canvas.DrawLine(x, (float)plot.Y, x, (float)plot.Bottom, paint);
            }
        }
    }

    private void DrawSegments(SKCanvas canvas, StateTimelineLayout layout, StateTimelineInterpolator display, StateTimelineInteraction interaction, StateTimelineRenderState state, ChartStyle style, float scale)
    {
        var plot = layout.Plot;
        var alpha = display.Alpha;
        var grow = (float)display.Scale;
        if ((alpha <= 0) || (grow <= 0) || !(layout.RowHeight > 0) || !(plot.Width > 0))
        {
            return;
        }

        var axis = layout.Axis;
        var maximum = axis.DisplayScale.Maximum;
        var pixel = scale > 0 ? 1 / scale : 1;
        var lineWidth = (float)state.LineWidth;
        var size = (float)style.DataLabelFontSize;
        var metrics = text.GetMetrics(size);
        canvas.Save();
        try
        {
            canvas.ClipRect(new SKRect((float)plot.X, (float)plot.Y, (float)(plot.X + (plot.Width * display.RevealRatio)), (float)plot.Bottom));
            var segments = layout.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var left = Snap(Math.Max(axis.ToDisplayPixel(segment.Start), plot.X - pixel), scale);
                var right = Snap(Math.Min(axis.ToDisplayPixel(StateTimelineLayout.GetEnd(segment, maximum)), plot.Right + pixel), scale);
                if ((right < plot.X) || (left > plot.Right))
                {
                    continue;
                }

                var top = layout.GetRowTop(segment.Row);
                var bottom = top + layout.RowHeight;
                if (grow < 1)
                {
                    var middle = (top + bottom) / 2;
                    top = middle - ((middle - top) * grow);
                    bottom = middle + ((bottom - middle) * grow);
                }

                var rect = new SKRect(left, Snap(top, scale), Math.Max(left + pixel, right), Math.Max(Snap(top, scale) + pixel, Snap(bottom, scale)));
                var color = layout.States[segment.State].Color;
                canvas.DrawRect(rect, paints.Fill(ChartColor.Fade(GetFill(color, state.FillOpacity), alpha)));
                if (lineWidth > 0)
                {
                    var inset = rect;
                    inset.Inflate(-lineWidth / 2, -lineWidth / 2);
                    canvas.DrawRect(inset, paints.Stroke(ChartColor.Fade(color, alpha), lineWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter));
                }

                if (interaction.IsTracking && (interaction.TrackedSegment == i))
                {
                    canvas.DrawRect(rect, paints.Fill(SKColors.White.WithAlpha(HoverAlpha)));
                }

                if (state.ShowValue != ChartVisibilityMode.Never)
                {
                    DrawValue(canvas, layout.States[segment.State].Text, rect, plot, color, state, lineWidth, size, metrics, alpha);
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawValue(SKCanvas canvas, string value, SKRect rect, ChartRect plot, SKColor color, StateTimelineRenderState state, float lineWidth, float size, SKFontMetrics metrics, double alpha)
    {
        if (value.Length == 0)
        {
            return;
        }

        var inset = lineWidth + TextPadding;
        var start = Math.Max(rect.Left, (float)plot.X);
        var end = Math.Min(rect.Right, (float)plot.Right);
        var available = end - start - (inset * 2);
        if (available <= 0)
        {
            return;
        }

        if ((state.ShowValue == ChartVisibilityMode.Auto) && (spans.Measure(text, value.AsSpan(0, Math.Min(MinCharacters, value.Length)), size) > available))
        {
            return;
        }

        var drawable = text.Create(value, size, available);
        var x = state.AlignValue switch
        {
            ChartValueAlignment.Center => ((start + end) / 2) - (drawable.Width / 2),
            ChartValueAlignment.Right => end - inset - drawable.Width,
            _ => start + inset
        };
        var baseline = rect.MidY - ((metrics.Descent - metrics.Ascent) / 2) - metrics.Ascent;
        text.Draw(canvas, drawable, x, baseline, SKTextAlign.Left, ChartColor.Fade(ChartColor.GetContrastColor(color), alpha), SKColors.Transparent, 0);
    }

    private void DrawLabels(SKCanvas canvas, StateTimelineLayout layout, ChartStyle style)
    {
        var color = style.MutedTextColor.ToSKColor();
        var labels = layout.RowLabels;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            text.DrawText(canvas, label.Text, label.Bounds, style.AxisLabelFontSize, label.MaxWidth, color, style);
        }

        var axis = layout.Axis;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < axis.Labels.Count; i++)
        {
            var label = axis.Labels[i];
            if (axis.IsDisplayed(label.Value))
            {
                text.DrawText(canvas, label.Text, axis.GetDisplayBounds(label), style.AxisLabelFontSize, label.MaxWidth, color, style);
            }
        }
    }

    private void DrawCursor(SKCanvas canvas, StateTimelineLayout layout, StateTimelineInteraction interaction, ChartStyle style)
    {
        var plot = layout.Plot;
        if (!interaction.IsTracking || plot.IsEmpty)
        {
            return;
        }

        var x = (float)Math.Clamp(interaction.TrackPoint.X, plot.X, plot.Right);
        canvas.DrawLine(x, (float)plot.Y, x, (float)plot.Bottom, paints.Stroke(style.TrackballLineColor.ToSKColor(), 0, SKStrokeCap.Butt));
    }

    private void DrawLegend(SKCanvas canvas, StateTimelineLayout layout, ChartStyle style)
    {
        var legend = layout.Legend;
        if (!legend.IsVisible)
        {
            return;
        }

        var color = style.SecondaryTextColor.ToSKColor();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < legend.Items.Count; i++)
        {
            var item = legend.Items[i];
            var marker = item.Marker;
            canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Height / 2, paints.Fill(layout.LegendColors[item.SeriesIndex]));
            text.DrawText(canvas, layout.LegendTexts[item.SeriesIndex], item.Text, style.LegendFontSize, item.TextMaxWidth, color, style);
        }

        if (legend.OverflowCount > 0)
        {
            text.DrawText(canvas, legend.OverflowText, legend.OverflowBounds, style.LegendFontSize, Double.PositiveInfinity, style.MutedTextColor.ToSKColor(), style);
        }
    }

    private void DrawTooltip(SKCanvas canvas, StateTimelineInteraction interaction, ChartStyle style, float width, float height)
    {
        if (!interaction.IsTracking || !interaction.TooltipVisible || (interaction.TooltipHeader is not { } header))
        {
            return;
        }

        ChartTooltipPainter.Draw(canvas, text, paints, header, interaction.TrackedTexts, interaction.TrackedColors, interaction.TrackPoint, style, width, height);
    }
}
