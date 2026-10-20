namespace ClamChart.Rendering;

using SkiaSharp;

internal sealed class PaintCache : IDisposable
{
    private readonly SKPaint stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

    public SKPaint Clear { get; } = new() { IsAntialias = true, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Clear };

    public SKPathBuilder Path { get; } = new();

    public void Dispose()
    {
        stroke.Dispose();
        fill.Dispose();
        Clear.Dispose();
        Path.Dispose();
    }

    public SKPaint Stroke(SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round, SKStrokeJoin join = SKStrokeJoin.Round)
    {
        stroke.PathEffect = null;
        stroke.Color = color;
        stroke.StrokeWidth = width;
        stroke.StrokeCap = cap;
        stroke.StrokeJoin = join;
        return stroke;
    }

    public SKPaint Fill(SKColor color)
    {
        fill.Shader = null;
        fill.Color = color;
        return fill;
    }
}
