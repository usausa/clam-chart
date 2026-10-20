namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaExtension;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class ChartTextPainter : IChartTextMeasurer, IDisposable
{
    private readonly FontResolver fonts;
    private readonly SKPaint fillPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint haloPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Round };

    public int Measurements => fonts.Measurements;

    public ChartTextPainter(string fontFamily)
    {
        fonts = new FontResolver(fontFamily);
    }

    public void Dispose()
    {
        fonts.Dispose();
        fillPaint.Dispose();
        haloPaint.Dispose();
    }

    public DrawableText Create(string text, float size, float maxWidth = Single.PositiveInfinity) =>
        fonts.CreateText(text, size, false, maxWidth);

    public SKFontMetrics GetMetrics(float size) => fonts.GetMetrics(size, false);

    public double Measure(string text, double fontSize, double maxWidth) => fonts.CreateText(text, (float)fontSize, false, (float)maxWidth).Width;

    public double GetLineHeight(double fontSize) => fonts.GetLineHeight((float)fontSize, false);

    public void DrawText(SKCanvas canvas, string value, ChartRect bounds, double fontSize, double maxWidth, SKColor color, ChartStyle style, bool halo = true)
    {
        var size = (float)fontSize;
        var drawable = Create(value, size, (float)maxWidth);
        var metrics = GetMetrics(size);
        var baseline = (float)bounds.Y + (((float)bounds.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        Draw(canvas, drawable, (float)bounds.X, baseline, SKTextAlign.Left, color, halo ? style.TextHaloColor.ToSKColor() : SKColors.Transparent, halo ? (float)style.TextHaloWidth : 0);
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style)
    {
        var size = style.LegendFontSize;
        var width = Measure(value, size, bounds.Width);
        var height = GetLineHeight(size);
        DrawText(canvas, value, new ChartRect(bounds.CenterX - (width / 2), bounds.CenterY - (height / 2), width, height), size, bounds.Width, style.MutedTextColor.ToSKColor(), style);
    }

    public void Draw(SKCanvas canvas, DrawableText text, float x, float baseline, SKTextAlign align, SKColor color, SKColor haloColor, float haloWidth)
    {
        if ((haloWidth > 0) && (haloColor.Alpha > 0))
        {
            DrawHalo(canvas, text, x, baseline, align, haloColor, haloWidth);
        }

        DrawFill(canvas, text, x, baseline, align, color);
    }

    public void DrawHalo(SKCanvas canvas, DrawableText text, float x, float baseline, SKTextAlign align, SKColor color, float width)
    {
        haloPaint.Color = color;
        haloPaint.StrokeWidth = width * 2;
        canvas.DrawText(text, x, baseline, align, haloPaint);
    }

    public void DrawFill(SKCanvas canvas, DrawableText text, float x, float baseline, SKTextAlign align, SKColor color)
    {
        fillPaint.Color = color;
        canvas.DrawText(text, x, baseline, align, fillPaint);
    }
}
