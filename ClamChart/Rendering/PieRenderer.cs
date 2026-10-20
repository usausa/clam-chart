namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class PieRenderer : IDisposable
{
    private const double Tolerance = 1e-6;

    private const double LabelPadding = 2;

    private const float GapOverlap = 1;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;

    public IChartTextMeasurer Measurer => text;

    public PieRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, PieLayout layout, PieInterpolator display, PieInteraction interaction, IReadOnlyList<Color> palette, ChartStyle style)
    {
        var progress = interaction.SelectionProgress < 1 ? display.Ease(interaction.SelectionProgress) : 1;
        var current = display.FindKey(interaction.SelectedKey) >= 0 ? interaction.SelectedKey : -1;
        var previous = (progress < 1) && (display.FindKey(interaction.PreviousKey) >= 0) ? interaction.PreviousKey : -1;
        var selection = new PieSelection(previous, current, progress, style.DimmedOpacity, interaction.ExplodeOffset);
        DrawSlices(canvas, layout, display, palette, style, selection);
        DrawLabels(canvas, layout, display, palette, style, selection);
        DrawCenter(canvas, layout, display, style, selection);
        DrawLegend(canvas, layout, palette, style);
    }

    private static SKColor GetColor(PieColorSource source, IReadOnlyList<Color> palette, ChartStyle style)
    {
        if (source.Color is { } color)
        {
            return color.ToSKColor();
        }

        return (!source.IsOthers && (source.Rank >= 0) && (source.Rank < palette.Count) ? palette[source.Rank] : style.OtherColor).ToSKColor();
    }

    private static SKColor GetColor(PieInterpolator display, int index, IReadOnlyList<Color> palette, ChartStyle style)
    {
        var target = display.GetColor(index);
        var previous = display.GetPreviousColor(index);
        var progress = display.ColorProgress;
        var color = GetColor(target, palette, style);
        return (progress >= 1) || (previous == target) ? color : ChartColor.Lerp(GetColor(previous, palette, style), color, progress);
    }

    private static (double X, double Y) GetOffset(PieLayout layout, double start, double end, double distance)
    {
        if (distance <= 0)
        {
            return default;
        }

        var (x, y) = PieLayout.GetDirection(layout.GetAngle((start + end) / 2));
        return (x * distance, y * distance);
    }

    private static bool IsCovered(PieInterpolator display)
    {
        var start = Double.PositiveInfinity;
        var end = Double.NegativeInfinity;
        for (var i = 0; i < display.Count; i++)
        {
            var (from, to) = display.GetSlice(i);
            if (to - from > Tolerance)
            {
                start = Math.Min(start, from);
                end = Math.Max(end, to);
            }
        }

        return (start <= Tolerance) && (end >= 1 - Tolerance);
    }

    private static void ClipGap(SKCanvas canvas, float cx, float cy, float outer, double clockAngle, float half, bool clockwise)
    {
        var degrees = (float)(clockAngle - 90);
        canvas.RotateDegrees(degrees, cx, cy);
        var rect = clockwise ? new SKRect(cx - half, cy - GapOverlap, cx + outer + GapOverlap, cy + half) : new SKRect(cx - half, cy - half, cx + outer + GapOverlap, cy + GapOverlap);
        canvas.ClipRect(rect, SKClipOperation.Difference, true);
        canvas.RotateDegrees(-degrees, cx, cy);
    }

    private static bool Fits(PieLayout layout, double start, double end, ChartRect bounds)
    {
        var startAngle = layout.GetAngle(start);
        var sweep = layout.SweepAngle * (end - start);
        if (sweep <= 0)
        {
            return false;
        }

        var nearestX = Math.Clamp(layout.CenterX, bounds.X, bounds.Right);
        var nearestY = Math.Clamp(layout.CenterY, bounds.Y, bounds.Bottom);
        if (Math.Sqrt(Square(nearestX - layout.CenterX) + Square(nearestY - layout.CenterY)) < layout.InnerRadius + LabelPadding)
        {
            return false;
        }

        var clearance = (layout.Gap / 2) + LabelPadding;
        return IsInside(layout, bounds.X, bounds.Y, startAngle, sweep, clearance) && IsInside(layout, bounds.Right, bounds.Y, startAngle, sweep, clearance) && IsInside(layout, bounds.X, bounds.Bottom, startAngle, sweep, clearance) && IsInside(layout, bounds.Right, bounds.Bottom, startAngle, sweep, clearance);
    }

    private static bool IsInside(PieLayout layout, double x, double y, double startAngle, double sweep, double clearance)
    {
        var dx = x - layout.CenterX;
        var dy = y - layout.CenterY;
        if (Math.Sqrt(Square(dx) + Square(dy)) > layout.OuterRadius - LabelPadding)
        {
            return false;
        }

        if (sweep >= 360 - Tolerance)
        {
            return true;
        }

        if (PieLayout.Normalize(PieLayout.GetClockAngle(dx, dy) - startAngle) > sweep)
        {
            return false;
        }

        return (GetRayDistance(dx, dy, startAngle) >= clearance) && (GetRayDistance(dx, dy, startAngle + sweep) >= clearance);
    }

    private static double GetRayDistance(double dx, double dy, double clockAngle)
    {
        var (x, y) = PieLayout.GetDirection(clockAngle);
        var along = (dx * x) + (dy * y);
        return along <= 0 ? Math.Sqrt(Square(dx) + Square(dy)) : Math.Abs((dx * y) - (dy * x));
    }

    private static double Square(double value) => value * value;

    private static PieCenterText GetCenter(PieLayout layout, int key)
    {
        var slice = layout.FindSlice(key);
        if ((slice >= 0) && (slice < layout.SliceCenters.Count) && layout.SliceCenters[slice].IsVisible)
        {
            return layout.SliceCenters[slice];
        }

        return layout.Center;
    }

    private void DrawSlices(SKCanvas canvas, PieLayout layout, PieInterpolator display, IReadOnlyList<Color> palette, ChartStyle style, PieSelection selection)
    {
        if (layout.OuterRadius <= 0)
        {
            return;
        }

        var reveal = display.RevealLimit;
        var entrance = display.EntranceAlpha;
        var circle = layout.SweepAngle >= 360;
        var wrap = circle && IsCovered(display);
        for (var i = 0; i < display.Count; i++)
        {
            var (start, end) = display.GetSlice(i);
            var drawn = Math.Min(end, reveal);
            if (drawn - start <= Tolerance)
            {
                continue;
            }

            var key = display.GetKey(i);
            var alpha = entrance * selection.Alpha(key);
            if (alpha <= 0)
            {
                continue;
            }

            var full = circle && (drawn - start >= 1 - Tolerance);
            var startGap = !full && ((start > Tolerance) || wrap);
            var endGap = !full && ((end < 1 - Tolerance) || wrap);
            var (dx, dy) = GetOffset(layout, start, end, selection.Offset(key));
            DrawSlice(canvas, layout, start, drawn, end, startGap, endGap, (float)dx, (float)dy, ChartColor.Fade(GetColor(display, i, palette, style), alpha));
        }
    }

    private void DrawSlice(SKCanvas canvas, PieLayout layout, double start, double drawn, double end, bool startGap, bool endGap, float dx, float dy, SKColor color)
    {
        var cx = (float)layout.CenterX;
        var cy = (float)layout.CenterY;
        var outer = (float)layout.OuterRadius;
        var inner = (float)layout.InnerRadius;
        var half = (float)layout.Gap / 2;
        var startAngle = layout.GetAngle(start);
        var sweep = (float)(layout.SweepAngle * (drawn - start));
        canvas.Save();
        canvas.Translate(dx, dy);
        if (half > 0)
        {
            if (startGap)
            {
                ClipGap(canvas, cx, cy, outer, startAngle, half, true);
            }

            if (endGap)
            {
                ClipGap(canvas, cx, cy, outer, layout.GetAngle(end), half, false);
            }
        }

        var full = sweep >= 360 - Tolerance;
        if (inner > 0)
        {
            var radius = (outer + inner) / 2;
            var paint = paints.Stroke(color, outer - inner, SKStrokeCap.Butt, SKStrokeJoin.Miter);
            if (full)
            {
                canvas.DrawCircle(cx, cy, radius, paint);
            }
            else
            {
                canvas.DrawArc(new SKRect(cx - radius, cy - radius, cx + radius, cy + radius), (float)(startAngle - 90), sweep, false, paint);
            }
        }
        else
        {
            var paint = paints.Fill(color);
            if (full)
            {
                canvas.DrawCircle(cx, cy, outer, paint);
            }
            else
            {
                canvas.DrawArc(new SKRect(cx - outer, cy - outer, cx + outer, cy + outer), (float)(startAngle - 90), sweep, true, paint);
            }
        }

        canvas.Restore();
    }

    private void DrawLabels(SKCanvas canvas, PieLayout layout, PieInterpolator display, IReadOnlyList<Color> palette, ChartStyle style, PieSelection selection)
    {
        var overlay = display.OverlayAlpha;
        if (!layout.ShowsLabels || (overlay <= 0) || (layout.OuterRadius <= 0))
        {
            return;
        }

        var reveal = display.RevealLimit;
        var height = layout.LabelHeight;
        var radius = layout.GetLabelRadius();
        for (var i = 0; i < display.Count; i++)
        {
            var target = display.GetTarget(i);
            if ((target < 0) || (layout.Slices[target].InsideText is not { } label))
            {
                continue;
            }

            var (start, end) = display.GetSlice(i);
            if (end > reveal + Tolerance)
            {
                continue;
            }

            var width = layout.Slices[target].InsideWidth;
            var (x, y) = PieLayout.GetDirection(layout.GetAngle((start + end) / 2));
            var bounds = new ChartRect(layout.CenterX + (x * radius) - (width / 2), layout.CenterY + (y * radius) - (height / 2), width, height);
            if (!Fits(layout, start, end, bounds))
            {
                continue;
            }

            var key = display.GetKey(i);
            var (dx, dy) = GetOffset(layout, start, end, selection.Offset(key));
            var color = ChartColor.GetContrastColor(GetColor(display, i, palette, style));
            text.DrawText(canvas, label, bounds with { X = bounds.X + dx, Y = bounds.Y + dy }, style.DataLabelFontSize, Double.PositiveInfinity, ChartColor.Fade(color, overlay * selection.Alpha(key)), style);
        }
    }

    private void DrawCenter(SKCanvas canvas, PieLayout layout, PieInterpolator display, ChartStyle style, PieSelection selection)
    {
        var overlay = display.OverlayAlpha;
        if (overlay <= 0)
        {
            return;
        }

        var current = GetCenter(layout, selection.Current);
        if (selection.Progress < 1)
        {
            var previous = GetCenter(layout, selection.Previous);
            if (previous != current)
            {
                DrawCenterText(canvas, previous, overlay * (1 - selection.Progress), style);
                DrawCenterText(canvas, current, overlay * selection.Progress, style);
                return;
            }
        }

        DrawCenterText(canvas, current, overlay, style);
    }

    private void DrawCenterText(SKCanvas canvas, PieCenterText center, double alpha, ChartStyle style)
    {
        if ((center.Value is not { } value) || (alpha <= 0))
        {
            return;
        }

        text.DrawText(canvas, value, center.ValueBounds, center.ValueFontSize, Double.PositiveInfinity, ChartColor.Fade(style.PrimaryTextColor.ToSKColor(), alpha), style);
        if (center.Caption is { } caption)
        {
            text.DrawText(canvas, caption, center.CaptionBounds, center.CaptionFontSize, Double.PositiveInfinity, ChartColor.Fade(style.SecondaryTextColor.ToSKColor(), alpha), style);
        }
    }

    private void DrawLegend(SKCanvas canvas, PieLayout layout, IReadOnlyList<Color> palette, ChartStyle style)
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
            var slice = layout.Slices[item.SeriesIndex];
            var marker = item.Marker;
            canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Height / 2, paints.Fill(GetColor(slice.ColorSource, palette, style)));
            text.DrawText(canvas, slice.Label ?? String.Empty, item.Text, style.LegendFontSize, item.TextMaxWidth, color, style);
            if (!item.Value.IsEmpty)
            {
                var value = layout.LegendValueMode == ChartLegendValueMode.Percent ? slice.PercentText : slice.ValueText;
                text.DrawText(canvas, value, item.Value, style.LegendFontSize, Double.PositiveInfinity, color, style);
            }
        }

        if (legend.OverflowCount > 0)
        {
            text.DrawText(canvas, legend.OverflowText, legend.OverflowBounds, style.LegendFontSize, Double.PositiveInfinity, style.MutedTextColor.ToSKColor(), style);
        }
    }
}
