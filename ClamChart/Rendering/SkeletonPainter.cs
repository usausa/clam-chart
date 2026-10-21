namespace ClamChart.Rendering;

using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class SkeletonPainter : IDisposable
{
    private const float BandRatio = 0.4f;

    private const float BarRatio = 0.6f;

    private const float RingRatio = 0.3f;

    private const float RowThickness = 10;

    private const float TitleHeight = 10;

    private const float TitleRatio = 0.4f;

    private const float ValueHeight = 24;

    private const float ValueRatio = 0.55f;

    private const float TileGap = 8;

    private static readonly float[] LineLevels = [0.62f, 0.4f, 0.5f, 0.28f, 0.42f, 0.22f, 0.34f];

    private static readonly float[] BarLevels = [0.45f, 0.7f, 0.55f, 0.85f, 0.6f, 0.75f, 0.5f];

    private static readonly float[] RowLevels = [0.85f, 0.6f, 0.72f];

    private readonly SKPathBuilder builder = new();
    private readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint band = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private SKPath? path;
    private SKShader? shader;
    private (ChartRect Bounds, SkeletonShape Shape, float Thickness, float MaxBarWidth) pathKey;
    private (float Width, SKColor Color) shaderKey;

    public void Dispose()
    {
        builder.Dispose();
        fill.Dispose();
        band.Dispose();
        path?.Dispose();
        shader?.Dispose();
    }

    public void Draw(SKCanvas canvas, ChartRect bounds, SkeletonShape shape, ChartStyle style, double phase)
    {
        if ((bounds.Width <= 0) || (bounds.Height <= 0))
        {
            return;
        }

        var current = EnsurePath(bounds, shape, (float)style.LineThickness * 2, (float)style.MaxBarWidth);
        fill.Color = style.SkeletonColor.ToSKColor();
        canvas.DrawPath(current, fill);
        if (Double.IsNaN(phase))
        {
            return;
        }

        var width = (float)bounds.Width * BandRatio;
        EnsureShader(width, style.SkeletonHighlightColor.ToSKColor());
        canvas.Save();
        canvas.ClipPath(current, SKClipOperation.Intersect, true);
        canvas.Translate((float)(bounds.X - width + ((bounds.Width + width) * phase)), (float)bounds.Y);
        canvas.DrawRect(0, 0, width, (float)bounds.Height, band);
        canvas.Restore();
    }

    private static SKPoint GetLinePoint(ChartRect bounds, int index, int last, float half) =>
        new((float)(bounds.X + (bounds.Width * index / last)), (float)(bounds.Y + half + ((bounds.Height - (half * 2)) * LineLevels[index])));

    private SKPath EnsurePath(ChartRect bounds, SkeletonShape shape, float thickness, float maxBarWidth)
    {
        var key = (bounds, shape, thickness, maxBarWidth);
        if ((path is not null) && (pathKey == key))
        {
            return path;
        }

        builder.FillType = shape == SkeletonShape.Ring ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
        switch (shape)
        {
            case SkeletonShape.Bars:
                AddBars(bounds, maxBarWidth);
                break;
            case SkeletonShape.Ring:
                AddRing(bounds);
                break;
            case SkeletonShape.Rows:
                AddRows(bounds);
                break;
            case SkeletonShape.Tile:
                AddTile(bounds);
                break;
            default:
                AddLine(bounds, thickness);
                break;
        }

        path?.Dispose();
        path = builder.Detach();
        pathKey = key;
        return path;
    }

    private void AddLine(ChartRect bounds, float thickness)
    {
        var half = Math.Min(thickness, (float)bounds.Height) / 2;
        var last = LineLevels.Length - 1;
        var start = GetLinePoint(bounds, 0, last, half);
        builder.MoveTo(start.X, start.Y - half);
        for (var i = 1; i <= last; i++)
        {
            var point = GetLinePoint(bounds, i, last, half);
            builder.LineTo(point.X, point.Y - half);
        }

        for (var i = last; i >= 0; i--)
        {
            var point = GetLinePoint(bounds, i, last, half);
            builder.LineTo(point.X, point.Y + half);
        }

        builder.Close();
    }

    private void AddBars(ChartRect bounds, float maxBarWidth)
    {
        var slot = (float)bounds.Width / BarLevels.Length;
        var half = Math.Min(slot * BarRatio, maxBarWidth) / 2;
        var bottom = (float)bounds.Bottom;
        for (var i = 0; i < BarLevels.Length; i++)
        {
            var center = (float)bounds.X + (slot * (i + 0.5f));
            builder.AddRect(new SKRect(center - half, bottom - ((float)bounds.Height * BarLevels[i]), center + half, bottom));
        }
    }

    private void AddRing(ChartRect bounds)
    {
        var radius = (float)Math.Min(bounds.Width, bounds.Height) / 2;
        builder.AddCircle((float)bounds.CenterX, (float)bounds.CenterY, radius);
        builder.AddCircle((float)bounds.CenterX, (float)bounds.CenterY, radius * (1 - RingRatio));
    }

    private void AddRows(ChartRect bounds)
    {
        var slot = (float)bounds.Height / RowLevels.Length;
        var thickness = Math.Min(RowThickness, slot * BarRatio);
        for (var i = 0; i < RowLevels.Length; i++)
        {
            var top = (float)bounds.Y + (slot * (i + 0.5f)) - (thickness / 2);
            builder.AddRect(new SKRect((float)bounds.X, top, (float)(bounds.X + (bounds.Width * RowLevels[i])), top + thickness));
        }
    }

    private void AddTile(ChartRect bounds)
    {
        var x = (float)bounds.X;
        var y = (float)bounds.Y;
        var height = (float)bounds.Height;
        var title = Math.Min(TitleHeight, height / 4);
        builder.AddRect(new SKRect(x, y, x + ((float)bounds.Width * TitleRatio), y + title));
        var top = y + title + Math.Min(TileGap, height / 8);
        builder.AddRect(new SKRect(x, top, x + ((float)bounds.Width * ValueRatio), top + Math.Min(ValueHeight, height / 2)));
    }

    private void EnsureShader(float width, SKColor color)
    {
        var key = (width, color);
        if ((shader is not null) && (shaderKey == key))
        {
            return;
        }

        shader?.Dispose();
        shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(width, 0), [color.WithAlpha(0), color, color.WithAlpha(0)], SKShaderTileMode.Clamp);
        band.Shader = shader;
        shaderKey = key;
    }
}
