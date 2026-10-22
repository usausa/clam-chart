namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal static class ChartTooltipPainter
{
    private const float Padding = 8;

    private const float RowGap = 2;

    private const float MarkerGap = 6;

    private const float Clearance = 48;

    public static void Draw(SKCanvas canvas, ChartTextPainter text, PaintCache paints, string header, IReadOnlyList<string?> texts, IReadOnlyList<SKColor> colors, Point finger, ChartStyle style, float width, float height)
    {
        var size = style.TooltipFontSize;
        var lineHeight = (float)text.GetLineHeight(size);
        var marker = (float)style.LegendMarkerSize;
        var rows = 1;
        var content = (float)text.Measure(header, size, Double.PositiveInfinity);
        for (var s = 0; (s < texts.Count) && (s < colors.Count); s++)
        {
            if (texts[s] is { } row)
            {
                rows++;
                content = Math.Max(content, marker + MarkerGap + (float)text.Measure(row, size, Double.PositiveInfinity));
            }
        }

        var boxWidth = content + (Padding * 2);
        var boxHeight = (rows * lineHeight) + ((rows - 1) * RowGap) + (Padding * 2);
        var left = boxWidth < width ? Math.Clamp((float)finger.X - (boxWidth / 2), 0, width - boxWidth) : 0;
        var top = (float)finger.Y - Clearance - boxHeight;
        if (top < 0)
        {
            top = (float)finger.Y + Clearance;
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
        var x = left + Padding;
        var y = top + Padding;
        text.DrawText(canvas, header, new ChartRect(x, y, content, lineHeight), size, Double.PositiveInfinity, color, style, false);
        for (var s = 0; (s < texts.Count) && (s < colors.Count); s++)
        {
            if (texts[s] is not { } row)
            {
                continue;
            }

            y += lineHeight + RowGap;
            canvas.DrawCircle(x + (marker / 2), y + (lineHeight / 2), marker / 2, paints.Fill(colors[s]));
            text.DrawText(canvas, row, new ChartRect(x + marker + MarkerGap, y, content - marker - MarkerGap, lineHeight), size, Double.PositiveInfinity, color, style, false);
        }
    }
}
